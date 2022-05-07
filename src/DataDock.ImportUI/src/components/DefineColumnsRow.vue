<template>
  <tr>
    <td>{{ value.name }}</td>
    <td colspan="3" v-if="value.virtual">
      <div class="ui info message">
        Virtual Column
      </div>
    </td>
    <td v-if="!value.virtual">
      <div class="ui field" v-bind:class="{ error: !titleValid }">
        <input
          type="text"
          v-model="value.titles[0]"
          @input="validateTitle"
          placeholder="A non-empty title is required"
        />
        <div class="ui error message" v-if="!titleValid">
          A non-empty title is required
        </div>
      </div>
    </td>
    <td v-if="!value.virtual">
      <select v-model="value.datatype">
        <option value="string">Text</option>
        <option value="uri">URI</option>
        <option value="integer">Whole Number</option>
        <option value="decimal">Decimal Number</option>
        <option value="date">Date</option>
        <option value="datetime">Date and Time</option>
        <option value="boolean">True/False</option>
        <option value="uriTemplate">URI Template</option>
      </select>
    </td>
    <td v-if="!value.virtual">
      <input type="checkbox" v-model="suppressed" />
    </td>
  </tr>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop } from "vue-property-decorator";

@Component
export default class DefineColumnsRow extends Vue {
  @Prop() private value: any;
  @Prop() private resourceIdentifierBase!: string;
  private titleValid: boolean = true;

  private get suppressed(): boolean {
    return this.value.columnType == "suppressed";
  }

  private set suppressed(newValue: boolean) {
    this.value.columnType = newValue ? "suppressed" : "standard";
  }

  private validateTitle() {
    const wasValid = this.titleValid;
    this.titleValid = this.value.titles[0].length > 0;
    if (this.titleValid != wasValid) {
      this.$emit("error", !this.titleValid);
    }
  }
}
</script>
