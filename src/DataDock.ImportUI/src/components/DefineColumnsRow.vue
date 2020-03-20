<template>
  <tr>
    <td>{{ value.name }}</td>
    <td>
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
    <td>
      <select v-model="value.datatype">
        <option value="string">Text</option>
        <option value="uri">URI</option>
        <option value="integer">Whole Number</option>
        <option value="decimal">Decimal Number</option>
        <option value="date">Date</option>
        <option value="dateTime">Date and Time</option>
        <option value="boolean">True/False</option>
        <option value="uriTemplate">URI Template</option>
      </select>
    </td>
    <td>
      <input type="checkbox" v-model="value.suppressOutput" />
    </td>
  </tr>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop, Watch } from "vue-property-decorator";

@Component
export default class DefineColumnsRow extends Vue {
  @Prop() private value: any;
  @Prop() private resourceIdentifierBase!: string;
  private titleValid: boolean = true;

  private validateTitle() {
    const wasValid = this.titleValid;
    if (this.value.titles[0].length > 0) {
      this.titleValid = true;
    } else {
      this.titleValid = false;
    }
    if (this.titleValid != wasValid) {
      this.$emit("error", !this.titleValid);
    }
  }
}
</script>
