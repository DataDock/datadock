<template>
  <div id="app">
    <div id="nav" class="ui top attached three steps">
      <router-link
        to="/"
        :class="{
          active: currentPage === 'choose',
          step: true,
          enabled: !loading
        }"
      >
        <i class="search icon" />
        <div class="content">
          <div class="title">Choose</div>
          <div class="description">Choose your CSV spreadsheet</div>
        </div>
      </router-link>
      <router-link
        to="/define"
        :class="{
          active: currentPage === 'define',
          step: true,
          disabled: !haveCsvFile
        }"
      >
        <i class="write icon" />
        <div class="content">
          <div class="title">
            Define
          </div>
          <div class="description">Enter license and other details</div>
        </div>
      </router-link>
      <router-link
        to="/upload"
        :class="{
          active: currentPage === 'upload',
          step: true,
          disabled: !haveCsvFile
        }"
      >
        <i class="cloud upload icon" />
        <div class="content">
          <div class="title">
            Upload
          </div>
          <div class="description">Upload to your data portal</div>
        </div>
      </router-link>
    </div>
    <router-view
      :templateMetadata="templateMetadata"
      :apiUrl="$root.$data.apiUrl"
      :baseUrl="$root.$data.baseUrl"
      :publishUrl="$root.$data.publishUrl"
      :ownerId="$root.$data.ownerId"
      :repoId="$root.$data.repoId"
      :csvFile="csvFile"
      :csvFileName="filename"
      :csvData="csvData"
      :importOptions="importOptions"
      :schema="schema"
      v-on:csv-loaded="onCsvLoaded"
      v-on:discard-template="onTemplateDiscarded"
    />
    <div class="attached segment" v-if="loading">
      <div class="ui active inverted dimmer">
        <div class="ui large text loader">Loading Schema</div>
      </div>
      <p></p>
    </div>
    <div class="attached segment" v-if="schemaError">
      <div class="ui error message">
        <div class="header">Error Loading Schema</div>
        <p>
          {{ schemaErrorMessage }}
        </p>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop } from "vue-property-decorator";
import { Helper, DatatypeSniffer, SnifferOptions } from "@/DataDock";
import Axios from "axios";

@Component
export default class App extends Vue {
  private csvFile: File | null = null;
  private filename: string = "";
  private csvData: string[][] = [];
  private templateMetadata: any = {};
  private header!: string[];
  private loading: boolean = false;
  private schemaError: boolean = false;
  private schemaErrorMessage: string = "";
  private schema: any = null;
  private importOptions: any = {
    appendData: false,
    saveTemplate: false,
    showOnHomePage: true
  };

  private get currentPage(): string | undefined {
    return this.$route.name;
  }

  private get haveCsvFile(): boolean {
    return this.csvFile !== null;
  }

  private get identifierBase(): string {
    return (
      this.$root.$data.publishUrl +
      "/" +
      this.$root.$data.ownerId +
      "/" +
      this.$root.$data.repoId +
      "/id"
    );
  }

  created() {
    if (this.$root.$data.schemaId != undefined) {
      this.loading = true;
      this.loadSchema();
    } else {
      this.loading = false;
    }
  }

  loadSchema() {
    let self = this;
    const schemaUrl = this.$root.$data.apiUrl + "/schemas";
    Axios.get(schemaUrl, {
      params: {
        ownerId: this.$root.$data.ownerId,
        schemaId: this.$root.$data.schemaId
      }
    }).then(response => {
      self.schema = response.data;
      self.loading = false;
    });
  }

  makeDatasetTitle(fileName: string): string {
    let fileTitle = fileName;
    if (fileTitle.includes(".")) {
      fileTitle = fileTitle.substring(0, fileTitle.lastIndexOf("."));
    }
    return fileTitle;
  }

  makeTemplateMetadata(data: string[][], file: File): any {
    const fileTitle = this.makeDatasetTitle(file.name);
    const datasetId = Helper.slugify(fileTitle);
    let templateMetadata: any = {
      "@context": "http://www.w3.org/ns/csvw",
      url: this.identifierBase + "/dataset/" + datasetId,
      "dc:title": fileTitle,
      "dc:description": "",
      "dc:license": "",
      "dcat:keyword": [],
      aboutUrl: this.identifierBase + "/resource/" + datasetId + "/row_{_row}",
      tableSchema: {}
    };
    templateMetadata.tableSchema.columns = [];
    let sniffer = new DatatypeSniffer(new SnifferOptions());
    let datatypes = sniffer.getDatatypes(data);

    for (let ix = 0; ix < data[0].length; ix++) {
      let col = data[0][ix];
      const colId = Helper.slugify(col);
      templateMetadata.tableSchema.columns.push({
        name: colId,
        titles: [col],
        datatype: datatypes[ix].sniffedType,
        propertyUrl: this.identifierBase + "/definition/" + colId
      });
    }
    this.ensureColumnDatatype(templateMetadata);
    return templateMetadata;
  }

  makeSchemaFromTemplate(data: string[][], file: File, template: any) {
    let fileTitle = this.makeDatasetTitle(file.name);
    let datasetId = Helper.slugify(fileTitle);
    let templateMetadata = Object.assign({}, template.schema.metadata);
    templateMetadata.url = this.identifierBase + "/dataset/" + datasetId;
    let baseUri =
      this.$root.$data.publishUrl +
      "/" +
      this.$root.$data.ownerId +
      "/" +
      this.$root.$data.repoId +
      "/";
    this.makeAbsolute(templateMetadata, baseUri);
    this.ensureColumnDatatype(templateMetadata);
    return templateMetadata;
  }

  ensureColumnDatatype(templateMetadata: any) {
    for (let i = 0; i < templateMetadata.tableSchema.columns.length; i++) {
      templateMetadata.tableSchema.columns[i] = Helper.addSchemaDatatype(
        templateMetadata.tableSchema.columns[i]
      );
    }
  }

  readonly colRefRegExp: RegExp = /^\{[^}]+\}$/;
  makeAbsolute(tok: any, baseUri: string) {
    if (Array.isArray(tok)) {
      tok.forEach(item => {
        this.makeAbsolute(item, baseUri);
      });
    } else if (typeof tok === "object" && tok !== null) {
      Object.keys(tok).forEach(k => {
        if (tok.hasOwnProperty(k)) {
          if (k === "aboutUrl" || k === "propertyUrl" || k === "valueUrl") {
            if (!tok[k].includes("://") && !this.colRefRegExp.test(tok[k])) {
              tok[k] = baseUri + tok[k];
            }
          } else {
            if (
              tok[k] != null &&
              (Array.isArray(tok[k]) || typeof tok[k] === "object")
            ) {
              this.makeAbsolute(tok[k], baseUri);
            }
          }
        }
      });
    }
  }

  onCsvLoaded(data: string[][], file: File, schema: any) {
    this.$data.csvData = data;
    this.$data.header = data[0];
    this.$data.csvFile = file;
    this.$data.filename = file.name;
    if (schema) {
      this.$data.templateMetadata = this.makeSchemaFromTemplate(
        data,
        file,
        schema
      );
    } else {
      this.$data.templateMetadata = this.makeTemplateMetadata(data, file);
    }
    this.$router.push({ path: "/define" });
  }

  onTemplateDiscarded() {
    this.schema = undefined;
  }
}
</script>
