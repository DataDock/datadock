<template>
  <div class="ui field" :class="{ error: !uriValid, required: required }">
    <label>{{ label }}</label>
    <input type="text" v-model="curieOrUri" />
    <div class="ui bottom attached">{{ value }}</div>
    <div class="ui error message" v-if="!uriValid">
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
  private curieOrUri: string = this.value;
  private usePrefixcc: boolean = true;
  private onDebouncedUriInput!: () => void;
  private uriValid: boolean = true;
  private errorMessage: string = "";

  created() {
    this.onDebouncedUriInput = _.debounce(this.expandCurie, 500);
  }

  getPrefixedPart(curie: string) {
    const prefixIx = curie.indexOf(":");
    return curie.substring(prefixIx + 1);
  }

  @Watch("curieOrUri")
  onUriInputChanged() {
    this.onDebouncedUriInput();
  }

  isCurie(): boolean {
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
    this.uriValid = false;
  }

  notifyValue(value: string) {
    this.$emit("input", value);
    if (!value.includes(":")) {
      this.uriValid = false;
      this.errorMessage = "Value must be a URI";
      this.$emit("error", false, { value: this.errorMessage });
    } else {
      this.uriValid = true;
      this.errorMessage = "";
      this.$emit("error", true, {});
    }
  }

  expandCurie() {
    if (this.isCurie()) {
      const prefixIx = this.curieOrUri.indexOf(":");
      const prefix = this.curieOrUri.substring(0, prefixIx);
      this.suffix = this.curieOrUri.substring(prefixIx + 1);
      if (prefix != this.prefix) {
        this.prefix = prefix;
        const self = this;
        Axios.get("https://prefix.cc/" + prefix + ".file.json")
          .then(function(response) {
            self.namespace = response.data[prefix];
            self.notifyValue(self.namespace + self.suffix);
          })
          .catch(function(error) {
            if (error.response.status == 404) {
              self.namespace = prefix + ":";
            } else {
              self.setError("Unable to expand compact URI prefix");
            }
          });
      } else {
        this.notifyValue(this.namespace + this.suffix);
      }
    } else {
      this.namespace = this.prefix + ":";
      this.notifyValue(this.curieOrUri);
    }
  }
}
</script>
