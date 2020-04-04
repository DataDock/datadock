<template>
  <div class="field" :class="{ error: hasErrors, required: required }">
    <label :for="name">{{ label }}</label>
    <input :id="name" type="text" v-model="uriTemplate" />
    <div class="ui error message" v-if="hasErrors">
      {{ errorMessage }}
    </div>
  </div>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop, Watch } from "vue-property-decorator";

@Component
export default class UriTemplateInput extends Vue {
  @Prop() private name!: string;
  @Prop() private label!: string;
  @Prop() private value!: string;
  @Prop() private required!: boolean;
  @Prop() private allowStatic!: boolean;
  @Prop() private templateMetadata!: any;
  private uriTemplate: string = this.value ?? "";
  private hasErrors: boolean = false;
  private errorMessage: string = "";
  private readonly templateRegex: RegExp = /{([^}]+)}/g;

  created() {
    this.validateUriTemplate();
  }

  private get uriTemplateValid(): boolean {
    this.validateUriTemplate();
    return this.hasErrors;
  }

  private setError(errorMessage: string) {
    this.errorMessage = errorMessage;
    this.hasErrors = true;
  }

  private hasColumn(columnRef: string): boolean {
    for (let col of this.templateMetadata.tableSchema.columns) {
      if (col.name === columnRef) return true;
    }
    return false;
  }

  @Watch("uriTemplate")
  private onUriTemplateChanged() {
    this.validateUriTemplate();
    this.$emit("input", this.uriTemplate);
    if (this.hasErrors) {
      this.$emit("error", this.name, this.hasErrors, this.errorMessage);
    } else {
      this.$emit("error", this.name, false, "");
    }
  }

  private validateUriTemplate() {
    let hadErrors = this.hasErrors;
    this.errorMessage = "";
    this.hasErrors = false;

    if (this.uriTemplate.length == 0 && this.required) {
      this.setError("A non-empty URI template string is required");
      return;
    }

    let results = this.uriTemplate.match(this.templateRegex);
    if (results) {
      for (let match of results) {
        let columnRef = match.substring(1, match.length - 1);
        if (columnRef !== "_row" && !this.hasColumn(columnRef)) {
          this.setError(
            "Template references a non-existant column with name " + match
          );
          return;
        }
      }
    } else {
      if (!this.allowStatic) {
        this.setError(
          "URI Template must reference one or more columns using {columnName} syntax"
        );
        return;
      }
    }
  }
}
</script>
