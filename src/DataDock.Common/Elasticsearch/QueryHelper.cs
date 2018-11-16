using Nest;
using System.Collections.Generic;
using TermQuery = Nest.TermQuery;

namespace DataDock.Common.Elasticsearch
{
    public static class QueryHelper
    {
        public static QueryContainer FilterByShowOnHomepage()
        {
            // only return those who have showOnHomepage set to true
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("showOnHomePage"),
                    Value = true
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }

        public static QueryContainer FilterByOwnerId(string ownerId)
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
        public static QueryContainer FilterByOwnerIds(string[] ownerIds)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermsQuery
                {
                    Field = new Field("ownerId"),
                    Terms = ownerIds
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }
        public static QueryContainer FilterByOwnerIds(string[] ownerIds, bool showHidden)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermsQuery
                {
                    Field = new Field("ownerId"),
                    Terms = ownerIds
                }
            };
            if (showHidden) return new BoolQuery {Filter = filterClauses};

            var filterHiddenDatasets = new TermQuery
            {
                Field = new Field("showOnHomePage"),
                Value = true
            };
            filterClauses.Add(filterHiddenDatasets);
            return new BoolQuery { Filter = filterClauses };
        }
        public static QueryContainer FilterByOwnerIdAndRepositoryId(string ownerId, string repositoryId)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                },
                new TermQuery
                {
                    Field = new Field("repositoryId"),
                    Value = repositoryId
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }
        public static QueryContainer FilterByOwnerIdAndRepositoryIds(string ownerId, string[] repositoryIds)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                },
                new TermsQuery
                {
                    Field = new Field("repositoryId"),
                    Terms = repositoryIds
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }
        public static QueryContainer FilterByOwnerIdAndRepositoryIds(string ownerId, string[] repositoryIds, bool showHidden)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                },
                new TermsQuery
                {
                    Field = new Field("repositoryId"),
                    Terms = repositoryIds
                }
            };
            if (showHidden) return new BoolQuery { Filter = filterClauses };

            var filterHiddenDatasets = new TermQuery
            {
                Field = new Field("showOnHomePage"),
                Value = true
            };
            filterClauses.Add(filterHiddenDatasets);
            return new BoolQuery { Filter = filterClauses };
        }
        public static QueryContainer FilterByOwnerIdAndRepositoryIdAndDatasetId(string ownerId, string repositoryId, string datasetId)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                },
                new TermQuery
                {
                    Field = new Field("repositoryId"),
                    Value = repositoryId
                },
                new TermQuery
                {
                    Field = new Field("datasetId"),
                    Value = datasetId
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }

        public static QueryContainer FilterByTags(string[] tags, bool matchAll, bool showHidden)
        {
            var filterClauses = new List<QueryContainer>();
            if (matchAll)
            {
                // and
                foreach (var tag in tags)
                {
                    filterClauses.Add(new TermQuery
                    {
                        Field = new Field("tags"),
                        Value = tag
                    });
                }
            }
            else
            {
                // or/contains
                filterClauses.Add(new TermsQuery
                {
                    Field = new Field("tags"),
                    Terms = tags
                });
            }
            if (showHidden) return new BoolQuery { Filter = filterClauses };

            var filterHiddenDatasets = new TermQuery
            {
                Field = new Field("showOnHomePage"),
                Value = true
            };
            filterClauses.Add(filterHiddenDatasets);
            return new BoolQuery { Filter = filterClauses };
        }
        public static QueryContainer FilterOwnerByTags(string ownerId, string[] tags, bool matchAll, bool showHidden)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                }
            };
            if (matchAll)
            {
                // and
                foreach (var tag in tags)
                {
                    filterClauses.Add(new TermQuery
                    {
                        Field = new Field("tags"),
                        Value = tag
                    });
                }
            }
            else
            {
                // or/contains
                filterClauses.Add(new TermsQuery
                {
                    Field = new Field("tags"),
                    Terms = tags
                });
            }
            
            if (showHidden) return new BoolQuery { Filter = filterClauses };

            var filterHiddenDatasets = new TermQuery
            {
                Field = new Field("showOnHomePage"),
                Value = true
            };
            filterClauses.Add(filterHiddenDatasets);
            return new BoolQuery { Filter = filterClauses };
        }
        public static QueryContainer FilterRepositoryByTags(string ownerId, string repositoryId, string[] tags, bool matchAll, bool showHidden)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                },
                new TermQuery
                {
                    Field = new Field("repositoryId"),
                    Value = repositoryId
                }
            };
            if (matchAll)
            {
                // and
                foreach (var tag in tags)
                {
                    filterClauses.Add(new TermQuery
                    {
                        Field = new Field("tags"),
                        Value = tag
                    });
                }
            }
            else
            {
                // or/contains
                filterClauses.Add(new TermsQuery
                {
                    Field = new Field("tags"),
                    Terms = tags 
                });
            }
            if (showHidden) return new BoolQuery { Filter = filterClauses };

            var filterHiddenDatasets = new TermQuery
            {
                Field = new Field("showOnHomePage"),
                Value = true
            };
            filterClauses.Add(filterHiddenDatasets);
            return new BoolQuery { Filter = filterClauses };
        }
    }
}
