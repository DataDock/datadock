<template>
  <div class="ui attached segment">
    <p>Choose your CSV spreadsheet</p>
    <p>{{ errorMessage }}</p>
    <CsvFileSelector v-on:csv-loaded="onCsvLoaded"></CsvFileSelector>
    <div class="ui info message" v-if="schema && useSchema">
      <div class="header">Using template for import</div>
      <p>
        The CSV import will be processed using the template
        <strong>{{ schema.schema["dc:title"] }}</strong>
        . This template expects a CSV file with the following columns:
      </p>
      <ul>
        <li
          v-for="col of schema.schema.metadata.tableSchema.columns"
          :key="col.name"
        >
          {{ col.titles[0] }}
        </li>
      </ul>
      <p>
        The column names in your import do not need to match these names, but
        but the columns must be in the same order as specified in the template.
      </p>
      <div class="ui button" v-on:click="discardTemplate">Discard Template</div>
    </div>
  </div>
</template>

<script lang="ts">
import { Component, Prop, Vue } from "vue-property-decorator";
import { parse, ParseConfig, ParseResult, ParseError } from "papaparse";
import CsvFileSelector from "@/components/CsvFileSelector.vue";

@Component({
  components: {
    CsvFileSelector
  }
})
export default class Choose extends Vue {
  @Prop() schema: any;
  useSchema: boolean = true;
  errorMessage: string = "";
  onCsvLoaded(data: any[], file: File) {
    this.$emit("csv-loaded", data, file, this.useSchema ? this.schema : null);
  }
  discardTemplate() {
    this.useSchema = false;
    this.$emit("discard-template");
  }
}
</script>
