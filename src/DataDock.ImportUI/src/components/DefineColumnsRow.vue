<template>
  <tr>
    <td>{{ col.name }}</td>
    <td>
      <div class="ui field" v-bind:class="{ error: !titleValid }">
        <input
          type="text"
          v-model="col.titles[0]"
          @input="validateTitle"
          placeholder="A non-empty title is required"
        />
        <div class="ui error message" v-if="!titleValid">
          A non-empty title is required
        </div>
      </div>
    </td>
    <td>
      <select v-model="datatype">
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
      <input type="checkbox" v-model="col.suppressOutput" />
    </td>
  </tr>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop, Watch } from "vue-property-decorator";

@Component
export default class DefineColumnsRow extends Vue {
  @Prop() private col: any;
  @Prop() private resourceIdentifierBase!: string;
  private datatype: string = this.getDatatypeId();
  private titleValid: boolean = true;

  private getDatatypeId(): string {
    if ("valueUrl" in this.col) {
      if (
        this.col.valueUrl.startsWith("{") &&
        this.col.valueUrl.endsWith("}")
      ) {
        return "uri";
      }
      return "uriTemplate";
    }
    return this.col.datatype;
  }

  private validateTitle() {
    const wasValid = this.titleValid;
    if (this.col.titles[0].length > 0) {
      this.titleValid = true;
    } else {
      this.titleValid = false;
    }
    if (this.titleValid != wasValid) {
      this.$emit("error", !this.titleValid);
    }
  }

  @Watch("datatype")
  private onDatatypeChanged() {
    switch (this.datatype) {
      case "uri":
        this.col.valueUrl = "{" + this.col.name + "}";
        delete this.col.datatype;
        break;
      case "uriTemplate":
        this.col.valueUrl =
          this.resourceIdentifierBase +
          "/" +
          this.col.name +
          "/{" +
          this.col.name +
          "}";
        delete this.col.datatype;
        break;
      default:
        this.col.datatype = this.datatype;
        delete this.col.valueUrl;
    }
  }
}
</script>
