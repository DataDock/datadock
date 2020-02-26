<template>
  <table class="ui celled form table" :class="{ error: hasErrors }">
    <thead>
      <tr>
        <th>Column</th>
        <th>Title</th>
        <th>Data Type</th>
        <th>Suppress</th>
      </tr>
    </thead>
    <tbody>
      <define-columns-row
        v-for="col in templateMetadata.tableSchema.columns"
        v-bind:col="col"
        v-bind:resourceIdentifierBase="resourceIdentifierBase"
        v-bind:key="col.name"
        @error="onError(col.name, $event)"
      ></define-columns-row>
    </tbody>
  </table>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop } from "vue-property-decorator";
import DefineColumnsRow from "@/components/DefineColumnsRow.vue";
import { ColumnInfo } from "@/DataDock";

@Component({
  components: {
    DefineColumnsRow
  }
})
export default class DefineColumns extends Vue {
  @Prop() private templateMetadata: any;
  @Prop() private identifierBase!: string;
  @Prop() private resourceIdentifierBase!: string;
  private errorColumns: string[] = [];

  private get hasErrors() {
    return this.errorColumns.length > 0;
  }

  private onError(columnName: string, errorFlag: boolean) {
    if (errorFlag) {
      if (!this.errorColumns.includes(columnName)) {
        this.errorColumns.push(columnName);
        this.$emit("error", true);
      }
    } else {
      if (this.errorColumns.includes(columnName)) {
        this.errorColumns.splice(this.errorColumns.indexOf(columnName));
        this.$emit("error", this.errorColumns.length > 0);
      }
    }
  }
}
</script>
