using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataDock.Common;
using DataDock.Common.Models;
using DataDock.Common.Stores;
using Nest;
using Serilog;

namespace DataDock.Common.Elasticsearch
{
    public class JobStore : IJobStore
    {
        private readonly IElasticClient _client;
        public JobStore(IElasticClient client, ApplicationConfiguration config)
        {
            var indexName = config.JobsIndexName;
            Log.Debug("Create JobStore. Index={indexName}", indexName);
            _client = client;
            // Ensure the index exists
            var indexExistsReponse = _client.IndexExists(indexName);
            if (!indexExistsReponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", indexName, typeof(JobInfo));
                var createIndexResponse = _client.CreateIndex(indexName, c => c.Mappings(
                    mappings => mappings.Map<JobInfo>(m => m.AutoMap(-1))));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", indexName, createIndexResponse.DebugInformation);
                    throw new DataDockException(
                        $"Could not create index {indexName} for JobStore. Cause: {createIndexResponse.DebugInformation}");
                }
            }

            _client.ConnectionSettings.DefaultIndices[typeof(JobInfo)] = indexName;

        }

        public async Task<JobInfo> SubmitImportJobAsync(ImportJobRequestInfo jobDescription)
        {
            var jobInfo = new JobInfo(jobDescription);
            return await SubmitJobAsync(jobInfo);
        }

        public async Task<JobInfo> SubmitDeleteJobAsync(DeleteJobRequestInfo jobRequest)
        {
            var jobInfo = new JobInfo(jobRequest);
            return await SubmitJobAsync(jobInfo);
        }

        public async Task<JobInfo> SubmitSchemaImportJobAsync(SchemaImportJobRequestInfo jobRequest)
        {
            var jobInfo = new JobInfo(jobRequest);
            return await SubmitJobAsync(jobInfo);
        }

        public async Task<JobInfo> SubmitSchemaDeleteJobAsync(SchemaDeleteJobRequestInfo jobRequest)
        {
            var jobInfo = new JobInfo(jobRequest);
            return await SubmitJobAsync(jobInfo);
        }

        private async Task<JobInfo> SubmitJobAsync(JobInfo jobInfo) { 
            var indexResponse = await _client.IndexDocumentAsync<JobInfo>(jobInfo);
            if (!indexResponse.IsValid)
            {
                throw new JobStoreException($"Failed to insert new job record: {indexResponse.DebugInformation}");
            }
            return jobInfo;
        }

        public async Task<JobInfo> GetJobInfoAsync(string jobId)
        {
            var getResponse = await _client.GetAsync<JobInfo>(jobId);
            if (getResponse.IsValid) return getResponse.Source;
            if (!getResponse.Found)
            {
                throw new JobNotFoundException(jobId);
            }
            throw new JobStoreException($"Failed to retrieve job record for jobId {jobId}: {getResponse.DebugInformation}");
        }

        public async Task UpdateJobInfoAsync(JobInfo updatedJobInfo)
        {
            updatedJobInfo.RefreshedTimestamp = DateTime.UtcNow.Ticks;
            var updateResponse = await _client.IndexDocumentAsync(updatedJobInfo);
            if (!updateResponse.IsValid)
            {
                throw new JobStoreException(
                    $"Failed to update job record for jobId {updatedJobInfo.JobId}: {updateResponse.DebugInformation}");
            }
        }

        public async Task<IEnumerable<JobInfo>> GetJobsForUser(string userId, int skip = 0, int take = 20)
        {
            var response = await _client.SearchAsync<JobInfo>(s => s
                .From(0).Query(q => q.Match(m => m.Field(f => f.UserId).Query(userId)))
            );

            if (!response.IsValid)
            {
                throw new JobStoreException(
                    $"Error retrieving jobs for user {userId}. Cause: {response.DebugInformation}");
            }
            if (response.Total < 1) throw new JobNotFoundException(userId);
            return response.Documents;
        }

        public async Task<IEnumerable<JobInfo>> GetJobsForOwner(string ownerId, int skip = 0, int take = 20)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));

            var search = new SearchDescriptor<JobInfo>()
                .Query(q => QueryHelper.FilterByOwnerId(ownerId))
                .Skip(skip)
                .Take(take)
                .Sort(s => s
                    .Field(f => f.Field("queuedAt").Order(SortOrder.Descending))); 

            var rawQuery = "";
#if DEBUG
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
#endif
            var response =
                await _client.SearchAsync<JobInfo>(search);

            if (!response.IsValid)
            {
                throw new JobStoreException(
                    $"Error retrieving jobs for owner {ownerId}. Cause: {response.DebugInformation}");
            }

            if (response.Total < 1)
            {
                Log.Warning($"No jobs found with query {rawQuery}");
                throw new JobNotFoundException(ownerId, "");
            }
            return response.Documents;
        }

        public async Task<IEnumerable<JobInfo>> GetJobsForRepository(string ownerId, string repositoryId, int skip = 0, int take = 20)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));
            if (repositoryId == null) throw new ArgumentNullException(nameof(repositoryId));

            var search = new SearchDescriptor<JobInfo>()
                .Query(
                    q => QueryHelper.FilterByOwnerIdAndRepositoryId(ownerId, repositoryId))
                .Skip(skip)
                .Take(take)
                .Sort(s => s
                    .Field(f => f.Field("queuedAt").Order(SortOrder.Descending)));

            var rawQuery = "";
#if DEBUG
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
#endif
            var response =
                await _client.SearchAsync<JobInfo>(search);

            if (!response.IsValid)
            {
                throw new JobStoreException(
                    $"Error retrieving jobs for repository '{repositoryId}' of owner {ownerId}. Cause: {response.DebugInformation}");
            }

            if (response.Total < 1)
            {
                Log.Warning($"No jobs found with query {rawQuery}");
                throw new JobNotFoundException(ownerId, repositoryId);
            }
            return response.Documents;
        }

        public async Task<JobInfo> GetNextJob()
        {
            // TODO: Should make sure that: (a) there aren't any jobs running for the same GitHub repository
            // (b) when we claim the job to work on it, no-one else grabbed it before us (i.e. update with If-Not-Modified) (issue #87)

            var queued = (int)JobStatus.Queued;
            var running = (int) JobStatus.Running;
            var runningResults = await _client.SearchAsync<JobInfo>(s => s
                .Query(q => q.Bool(b => b
                    .Filter(bf => bf
                        .Match(m => m
                            .Field(f => f.CurrentStatus)
                            .Query(running.ToString())
                        )))));
            

            var queuedJobsClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("currentStatus"),
                    Value = queued.ToString()
                }
            };
            var notTheseRepos = new List<QueryContainer>();
            if (runningResults.IsValid && runningResults.Hits.Count > 0)
            {
                var repoClauses = new List<QueryContainer>();
                foreach (var runningJobHit in runningResults.Hits)
                {
                    var runningJobInfo = runningJobHit.Source;
                    repoClauses.Add(new TermQuery
                    {
                        Field = new Field("repositoryId"), Value = runningJobInfo.RepositoryId
                    });
                }
                notTheseRepos.Add(new BoolQuery { MustNot = repoClauses });
            }
            var searchRequest = new SearchRequest<JobInfo>
            {
                Query = new BoolQuery { Must = queuedJobsClauses, Filter = notTheseRepos },
                Sort = new List<ISort>
                {
                    new SortField { Field = "queuedTimestamp", Order = SortOrder.Ascending }
                }
            };
            var queuedResults = await _client.SearchAsync<JobInfo>(searchRequest);

            if (!queuedResults.IsValid)
            {
                throw new JobStoreException(
                    $"Error retrieving next job. Cause: {queuedResults.DebugInformation}");
            }
            if (queuedResults.Hits.Any())
            {
                // Attempt to update the job document to mark it as running
                var hit = queuedResults.Hits.First();
                var resultVersion = hit.Version;
                var jobInfo = hit.Source;
                
                jobInfo.RefreshedTimestamp = DateTime.UtcNow.Ticks;
                jobInfo.StartedAt = DateTime.UtcNow;
                jobInfo.CurrentStatus = JobStatus.Running;
                var indexResponse = await _client.IndexAsync(jobInfo, desc => desc
                    .Id(jobInfo.JobId)
                    .Version(resultVersion));
                if (indexResponse.IsValid)
                {
                    return jobInfo;
                }
            }

            return null;
        }
        
        public async Task<bool> DeleteJobsForOwnerAsync(string ownerId)
        {
            var deleteResponse = await _client.DeleteByQueryAsync<JobInfo>(s => s.Query(q => QueryByOwnerId(q, ownerId)));
            if (!deleteResponse.IsValid)
            {
                throw new JobStoreException(
                    $"Failed to delete all jobs owned by {ownerId}");
            }
            return true;
        }

        private static QueryContainer QueryByOwnerId(QueryContainerDescriptor<JobInfo> q, string ownerId)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }
    }
}

