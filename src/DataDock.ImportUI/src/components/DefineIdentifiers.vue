<template>
  <div>
    <form class="ui form">
      <div class="field">
        <label for="datasetId">Dataset Identifier</label>
        <input id="datasetId" type="text" readonly v-model="value['url']" />
      </div>
      <div class="ui message">
        The identifier for the dataset is automatically constructed from the
        chosen title, the GitHub repository, and the GitHub user or organization
        you're uploading the data to.
      </div>
      <div class="field">
        <label for="idCol">Identifier Column</label>
        <select id="idCol" v-model="identifierColumn">
          <option value="row_{_row}">
            CSV Row Number
          </option>
          <option
            v-for="col of value.tableSchema.columns"
            v-bind:key="col.name"
            v-bind:value="col.name + '/{' + col.name + '}'"
          >
            {{ "titles" in col ? col.titles[0] : col.name }}
          </option>
        </select>
      </div>
    </form>
  </div>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop, Watch } from "vue-property-decorator";
import { Helper } from "@/DataDock";

@Component
export default class DefineIdentifiers extends Vue {
  @Prop() private value: any;
  @Prop() private identifierBase!: string;
  @Prop() private datasetId!: string;

  private get identifierColumn(): string {
    if ("aboutUrl" in this.value) {
      if (this.value.aboutUrl.endsWith("row_{_row}")) {
        return "row_{_row}";
      }
      if (this.value.aboutUrl.startsWith(this.identifierBase + "/resource/")) {
        return this.value.aboutUrl.substring(this.identifierBase.length + 10);
      }
    }
    return "row_{_row}";
  }

  private set identifierColumn(newValue: string) {
    if (newValue === "row_{_row}") {
      this.value["aboutUrl"] =
        this.identifierBase + "/resource/" + this.datasetId + "/" + newValue;
    } else {
      this.value["aboutUrl"] = this.identifierBase + "/resource/" + newValue;
    }
  }

  @Watch("datasetId") private onDatasetIdChanged() {
    this.value.aboutUrl =
      this.identifierColumn === "row_{_row}"
        ? this.identifierBase +
          "/resource/" +
          this.datasetId +
          "/" +
          this.identifierColumn
        : this.identifierBase + "/resource/" + this.identifierColumn;
  }
}
</script>
