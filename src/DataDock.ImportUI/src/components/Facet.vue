<template>
  <div class="ui segment">
    <button
      class="ui small compact icon basic red button"
      style="float: right"
      data-tooltip="Delete this facet"
      @click="onDeleteFacet"
    >
      <i class="icon trash"></i>
    </button>
    <prefixed-uri-input
      class="clear-right"
      :key="$vnode.key + '_propertyUrl'"
      label="Facet Property URI"
      :required="true"
      v-model="facet.propertyUrl"
      v-on:error="onInputError"
    ></prefixed-uri-input>
    <div class="field">
      <label :for="$vnode.key + '_datatype'">Datatype</label>
      <select :id="$vnode.key + '_datatype'" v-model="facet.datatype">
        <option value="string">Text</option>
        <option value="integer">Whole Number</option>
        <option value="decimal">Decimal Number</option>
        <option value="date">Date</option>
        <option value="dateTime">Date and Time</option>
        <option value="boolean">True/False</option>
      </select>
    </div>
    <div class="field" :class="{ error: !defaultValid }">
      <label :for="$vnode.key + '_default'">Facet Value</label>
      <input
        :id="$vnode.key + '_default'"
        type="text"
        v-model="facet.default"
      />
      <div class="ui error message" v-if="errors.default">
        {{ errors.default }}
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop, Watch } from "vue-property-decorator";
import PrefixedUriInput from "@/components/PrefixedUriInput.vue";
import { SnifferOptions } from "@/DataDock";

@Component({ components: { PrefixedUriInput } })
export default class Facet extends Vue {
  @Prop() private value: any;
  @Prop() private facetIndex!: number;
  private hasErrors: boolean = false;
  private errors: any = {};

  private get facet(): any {
    return this.value[this.facetIndex];
  }

  private get defaultValid(): boolean {
    let validator = new SnifferOptions();
    delete this.errors.default;
    switch (this.value.datatype) {
      case "integer":
        if (!validator.isInteger(this.value.default)) {
          this.errors.default = "Invalid integer value";
        }
        break;
      case "decimal":
        if (!validator.isDecimal(this.value.default)) {
          this.errors.default = "Invalid decimal value";
        }
        break;
      case "float":
        if (!validator.isFloat(this.value.default)) {
          this.errors.default = "Invalid floating point value";
        }
        break;
      case "date":
        if (!validator.isDate(this.value.default)) {
          this.errors.default = "Invalid date value";
        }
        break;
      case "dateTime":
        if (!validator.isDateTime(this.value.default)) {
          this.errors.default = "Invalid date/time value";
        }
        break;
      case "boolean":
        if (!validator.isBoolean(this.value.default)) {
          this.errors.default = "Invalid boolean value";
        }
        break;
      case "uri":
        if (!validator.isUri(this.value.default)) {
          this.errors.default = "Invalid URL";
        }
    }
    this.updateErrorFlag();
    return !this.errors.default;
  }

  private onDeleteFacet() {
    this.value.splice(this.facetIndex, 1);
    // Clear any errors associated with this facet
    this.$emit("error", this.$vnode.key, false, "");
  }

  private onInputError(
    elementName: string,
    hasError: boolean,
    errorMessage: string
  ) {
    if (hasError) {
      this.errors[elementName] = errorMessage;
    } else {
      delete this.errors[elementName];
    }
    this.updateErrorFlag();
  }

  private updateErrorFlag() {
    let oldState = this.hasErrors;
    this.hasErrors = false;
    for (let k in this.errors) {
      if (this.errors[k]) {
        this.hasErrors = true;
        break;
      }
    }
    if (oldState !== this.hasErrors) {
      this.$emit(
        "error",
        this.$vnode.key,
        this.hasErrors,
        this.hasErrors ? "Error in facet definition" : ""
      );
    }
  }
}
</script>
