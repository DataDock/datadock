<template>
  <div class="ui attached segment">
    <div class="ui error message" v-if="errors.length > 0">
      <div class="header">Error</div>
      <ul class="list">
        <li v-for="(error, ix) of errors" :key="'error_' + ix">{{ error }}</li>
      </ul>
    </div>
    <div class="ui form">
      <div class="inline field">
        <div class="ui checkbox">
          <input
            type="checkbox"
            id="appendData"
            v-model="importOptions.appendData"
          />
          <label for="appendData">
            Add to existing data if a dataset already exists (default is to
            overwrite existing data)
          </label>
        </div>
      </div>
      <div class="inline field">
        <div class="ui checkbox">
          <input
            type="checkbox"
            id="includeInSearch"
            v-model="importOptions.showOnHomePage"
          />
          <label for="includeInSearch">
            Include my published dataset in DataDock search results
          </label>
        </div>
      </div>
      <div class="inline field">
        <div class="ui checkbox">
          <input
            type="checkbox"
            id="saveTemplate"
            v-model="importOptions.saveTemplate"
          />
          <label for="saveTemplate">
            Save this information as a template for future imports
          </label>
        </div>
      </div>
      <button class="ui large primary button" @click="onPublishClicked">
        Publish Data
      </button>
    </div>
  </div>
</template>

<script lang="ts">
import { Component, Prop, Vue, Watch } from "vue-property-decorator";
import Axios from "axios";

@Component
export default class Upload extends Vue {
  @Prop() public importOptions: any;
  @Prop() public baseUrl!: string;
  @Prop() public apiUrl!: string;
  @Prop() public ownerId!: string;
  @Prop() public repoId!: string;
  @Prop() public csvFileName!: string;
  @Prop() public csvFile: File | undefined;
  @Prop() public templateMetadata: any;
  public errors: string[] = [];

  onPublishClicked() {
    this.errors.splice(0, this.errors.length);
    if (!this.csvFile) {
      this.errors.push("No CSV file has been chosen yet");
      return;
    }
    let formData = new FormData();
    formData.append("ownerId", this.ownerId);
    formData.append("repoId", this.repoId);
    formData.append("file", this.csvFile, this.csvFileName);
    formData.append("filename", this.csvFileName);
    formData.append("metadata", JSON.stringify(this.$props.templateMetadata));
    formData.append(
      "showOnHomePage",
      JSON.stringify(this.importOptions.showOnHomePage)
    );
    formData.append(
      "saveAsSchema",
      JSON.stringify(this.importOptions.saveTemplate)
    );
    formData.append(
      "overwriteExisting",
      JSON.stringify(!this.importOptions.appendData)
    );
    Axios.post(this.apiUrl + "/data", formData, {
      headers: {
        "Content-Type": "multipart/form-data"
      }
    })
      .then(response => {
        if (response && response.status === 200) {
          let jobsUrl =
            this.baseUrl +
            "/dashboard/jobs/" +
            this.ownerId +
            "/" +
            this.repoId;
          var jobIds = response.data["jobIds"];
          if (jobIds) {
            jobsUrl = jobsUrl + "/" + jobIds;
          } else {
            jobsUrl = jobsUrl + "/latest";
          }
          window.location.href = jobsUrl;
        } else {
          this.errors.push("There was an error submitting the job to DataDock");
        }
      })
      .catch(error => {
        if (error.response && "responseText" in error.response.data) {
          this.errors.push(
            "Publish API has reported an error: " + error.response.responseText
          );
        } else {
          this.errors.push("Publish API has returned an unspecified error.");
        }
      });
  }
}
</script>