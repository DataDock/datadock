<template>
  <div>
    <div class="ui info message" v-show="rowCount > pageSize">
      Your CSV file contains {{ rowCount }} rows. Only the first
      {{ pageSize }} are shown here.
    </div>
    <table class="ui celled table">
      <thead>
        <tr>
          <th
            v-for="colIx in nonVirtualColumnIndexes"
            v-bind:key="'col_' + colIx"
          >
            {{ templateMetadata.tableSchema.columns[colIx].titles[0] }}
          </th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(row, ix) in page" :key="'row_' + ix">
          <td
            v-for="(cell, cellIx) in row"
            :key="'row_' + ix + '_cell_' + cellIx"
          >
            {{ row[cellIx] }}
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop } from "vue-property-decorator";

@Component
export default class DefinePreview extends Vue {
  @Prop() private templateMetadata: any;
  @Prop() private data!: any[];
  @Prop() private pageSize!: number;

  get rowCount() {
    return this.data.length - 1;
  }

  get page() {
    return this.data.slice(1, this.pageSize);
  }

  get nonVirtualColumnIndexes(): number[] {
    let indexes = [];
    for (let i = 0; i < this.templateMetadata.tableSchema.columns.length; i++) {
      if (!this.templateMetadata.tableSchema.columns[i].virtual) {
        indexes.push(i);
      }
    }
    return indexes;
  }
}
</script>
