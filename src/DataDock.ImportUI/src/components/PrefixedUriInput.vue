<template>
  <div class="ui field" :class="{ error: hasError, required: required }">
    <label :for="$vnode.key + '_input'">{{ label }}</label>
    <input
      :id="$vnode.key + '_input'"
      :key="$vnode.key + '_input'"
      type="text"
      @blur="notifyValue"
      @input="onInputEdited"
      v-model="curieOrUri"
    />
    <div class="ui bottom attached">{{ expandedUri }}</div>
    <div class="ui error message" v-if="hasError">
      {{ this.errorMessage }}
    </div>
  </div>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop, Watch } from "vue-property-decorator";
import * as _ from "lodash";
import Axios from "axios";

@Component
export default class PrefixedUriInput extends Vue {
  @Prop() private label!: string;
  @Prop() private value!: string;
  @Prop() private required!: boolean;
  private namespace: string = "";
  private prefix!: string;
  private suffix!: string;
  private usePrefixcc: boolean = true;
  private onDebouncedUriInput!: () => void;
  private curieOrUri: string = this.value;
  private expandedUri: string = this.value;
  private hasError: boolean = false;
  private uriEdited: boolean = false;
  private errorMessage: string = "";

  created() {
    this.onDebouncedUriInput = _.debounce(this.expandCurie, 500);
  }

  getPrefixedPart(curie: string) {
    const prefixIx = curie.indexOf(":");
    return curie.substring(prefixIx + 1);
  }

  @Watch("value")
  private onValueChanged(newValue: string) {
    // If the model value has changed externally and the user has not edited it, update
    if (!this.uriEdited) {
      this.curieOrUri = newValue;
    }
  }

  @Watch("curieOrUri")
  private onCurieOrUriChanged() {
    this.onDebouncedUriInput();
  }

  private onInputEdited() {
    // When the user has made a change in the text input, set this flag so we don't reset to the model value
    this.uriEdited = true;
  }

  isCurie(): boolean {
    if (!this.curieOrUri) return false;
    if (this.curieOrUri.includes(":")) {
      if (
        this.curieOrUri.startsWith("http://") ||
        this.curieOrUri.startsWith("https://") ||
        this.curieOrUri.startsWith("urn:")
      ) {
        return false;
      }
      return true;
    } else {
      return false;
    }
  }

  setError(errorMessage: string) {
    this.errorMessage = errorMessage;
    this.hasError = true;
    this.$emit("error", this.$vnode.key, true, this.errorMessage);
  }

  clearError() {
    this.hasError = false;
    this.$emit("error", this.$vnode.key, false, "");
  }

  notifyValue() {
    this.$emit("input", this.expandedUri);
    if (this.expandedUri == "" && this.required){
      this.setError("A value is required");
    } else if (!this.expandedUri.includes(":")) {
      this.setError("Value must be a URI");
    } else {
      this.clearError();
    }
  }

  expandCurie() {
    if (this.curieOrUri && this.isCurie()) {
      const prefixIx = this.curieOrUri.indexOf(":");
      const prefix = this.curieOrUri.substring(0, prefixIx);
      this.suffix = this.curieOrUri.substring(prefixIx + 1);
      if (prefix != this.prefix) {
        this.prefix = prefix;
        const self = this;
        Axios.get("https://prefix.cc/" + prefix + ".file.json")
          .then(function(response) {
            self.namespace = response.data[prefix];
            self.expandedUri = self.namespace + self.suffix;
          })
          .catch(function(error) {
            if (error.response.status == 404) {
              self.namespace = prefix + ":";
            } else {
              self.setError("Unable to expand compact URI prefix");
            }
          });
      } else {
        this.expandedUri = this.namespace + this.suffix;
      }
    } else {
      this.namespace = "";
      this.expandedUri = this.curieOrUri ?? "";
    }
  }
}
</script>
