<template>
  <div>
    <table class="ui celled form table" :class="{ error: hasErrors }">
      <thead>
        <tr>
          <th>Column</th>
          <th>Configuration</th>
        </tr>
      </thead>
      <tbody>
        <define-advanced-row
          v-for="(col, ix) of value.tableSchema.columns"
          v-model="value.tableSchema.columns[ix]"
          v-bind:colIx="ix"
          :identifierBase="identifierBase"
          v-bind:resourceIdentifierBase="resourceIdentifierBase"
          v-bind:templateMetadata="value"
          v-bind:key="col.name"
          @error="onError(col.name, ...arguments)"
          @input="$emit('input', value)"
        ></define-advanced-row>
      </tbody>
    </table>
    <button class="ui button" @click="addVirtualColumn()">Add Column</button>
  </div>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop } from "vue-property-decorator";
import DefineAdvancedRow from "@/components/DefineAdvancedRow.vue";

@Component({
  components: {
    DefineAdvancedRow
  }
})
export default class DefineAdvanced extends Vue {
  @Prop() private value: any;
  @Prop() private identifierBase!: string;
  @Prop() private resourceIdentifierBase!: string;
  private hasErrors: boolean = false;
  private errorColumns: string[] = [];

  private onError(columnName: string, errorFlag: boolean, errorMap: any) {
    if (errorFlag) {
      if (!this.errorColumns.includes(columnName)) {
        this.errorColumns.push(columnName);
      }
    } else {
      if (this.errorColumns.includes(columnName)) {
        this.errorColumns.splice(this.errorColumns.indexOf(columnName));
      }
    }
    const hadErrors = this.hasErrors;
    this.hasErrors = this.errorColumns.length > 0;
    if (this.hasErrors != hadErrors) {
      this.$emit("error", this.hasErrors);
    }
  }

  private hasColumn(colName: string): boolean {
    return this.value.tableSchema.columns.some((c: any) => c.name === colName);
  }

  private getNextVirtualColumnName() {
    let prefix = "virtualCol";
    let index = 1;
    while (this.hasColumn(prefix + index)) {
      index += 1;
    }
    return prefix + index;
  }
  private addVirtualColumn() {
    let colName = this.getNextVirtualColumnName();
    this.value.tableSchema.columns.push({
      name: colName,
      virtual: true,
      datatype: "uriTemplate",
      propertyUrl: this.identifierBase + "/definition/" + colName
    });
    this.$emit("input", this.value);
  }
}
</script>
