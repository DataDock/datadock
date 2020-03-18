<template>
  <tr>
    <td class="top aligned">
      {{ value.name }}
    </td>
    <td>
      <div class="inline field">
        <label :for="value.name + '_suppress'">Suppress Output</label>
        <input
          :id="value.name + '_suppress'"
          type="checkbox"
          v-model="value.suppressOutput"
        />
      </div>
      <div class="ui required field" :class="{ error: !titleValid }">
        <label :for="value.name + '_title'">Title</label>
        <input
          :id="value.name + 'title'"
          type="text"
          v-model="value.titles[0]"
          @input="onTitleChanged"
        />
        <div class="ui error message" v-if="!titleValid">
          {{ errors.title }}
        </div>
      </div>
      <prefixed-uri-input
        label="Property URI"
        required="true"
        v-model="value.propertyUrl"
        v-on:error="onUriInputError"
      ></prefixed-uri-input>
      <div class="field">
        <label :for="value.name + '_datatype'">Datatype</label>
        <select :id="value.name + '_datatype'" v-model="value.datatype">
          <option value="string">Text</option>
          <option value="uri">URI</option>
          <option value="integer">Whole Number</option>
          <option value="decimal">Decimal Number</option>
          <option value="date">Date</option>
          <option value="dateTime">Date and Time</option>
          <option value="boolean">True/False</option>
          <option value="uriTemplate">URI Template</option>
        </select>
      </div>
      <div class="field" v-if="value.datatype == 'string'">
        <label :for="value.name + '_lang'">Language</label>
        <input
          :id="value.name + '_lang'"
          type="text"
          placeholder="e.g. en, fr, de-AT"
          v-model="value['lang']"
          @change="notifyChange"
        />
      </div>
      <div
        class="required field"
        :class="{ error: !uriTemplateValid }"
        v-if="value.datatype == 'uriTemplate'"
      >
        <label :for="value.name + '_uriTemplate'">URI Template String</label>
        <input
          :id="value.name + '_uriTemplate'"
          type="text"
          v-model="uriTemplate"
        />
        <div class="ui error message" v-if="!uriTemplateValid">
          {{ errors.uriTemplate }}
        </div>
      </div>
    </td>
  </tr>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop, Watch } from "vue-property-decorator";
import DefineColumnsRow from "@/components/DefineColumnsRow.vue";
import PrefixedUriInput from "@/components/PrefixedUriInput.vue";

@Component({
  components: {
    PrefixedUriInput
  }
})
export default class DefineAdvancedRow extends Vue {
  @Prop() private value: any;
  @Prop() private colIx!: number;
  @Prop() private resourceIdentifierBase!: string;
  @Prop() private templateMetadata: any;
  private uriTemplate: string =
    "valueUrl" in this.value ? this.value["valueUrl"] : "";
  private errors: any = {};
  private hasErrors: boolean | undefined;
  private _uriTemplateValid: boolean = true;

  private notifyChange() {
    this.$emit("input", this.value);
  }

/*
  @Watch("value") onValueChanged() {
    if (this.value.datatype === "uriTemplate") {
      this.validateUriTemplate();
    }
  }
*/
  @Watch("uriTemplate") onUriTemplateChanged() {
    this.validateUriTemplate();
    this.value.valueUrl = this.uriTemplate;
    this.notifyChange();
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
      this.$emit("error", this.hasErrors, this.errors);
    }
  }

  private onTitleChanged() {
    this.notifyChange();
  }

  private get titleValid() {
    delete this.errors.title;
    if (this.value.titles[0].length == 0) {
      this.errors.title = "A non-empty title is required";
    }
    this.updateErrorFlag();
    return !this.errors.title;
  }

  private get uriTemplateValid(): boolean {
    if (this.value.datatype === "uriTemplate") {
      this.validateUriTemplate();
      return this._uriTemplateValid;
    }
    return this._uriTemplateValid;
  }

  private onUriInputError(isValid: boolean, errors: any) {
    if (isValid) {
      delete this.errors.propertyUrl;
    } else {
      this.errors.propertyUrl = errors["value"];
    }
    this.updateErrorFlag();
  }

  private readonly templateRegex: RegExp = /{([^}]+)}/g;

  private hasColumn(colName: string) {
    for (let col of this.templateMetadata.tableSchema.columns) {
      if (col.name === colName) return true;
    }
    return false;
  }

  private validateUriTemplate() {
    delete this.errors.uriTemplate;
    if (this.uriTemplate.length == 0) {
      this.errors.uriTemplate = "A non-empty URI template string is required";
    }
    this._uriTemplateValid = !("uriTemplate" in this.errors);
    var results = this.uriTemplate.match(this.templateRegex);
    if (!results) {
      this.errors.uriTemplate =
        "URI Template must reference one or more columns using {columnName} syntax";
      this._uriTemplateValid = false;
    } else {
      for (let match of results) {
        let columnRef = match.substring(1, match.length - 1);
        if (!this.hasColumn(columnRef)) {
          this.errors.uriTemplate =
            "Template references a non-existant column with name " + match;
          this._uriTemplateValid = false;
        }
      }
    }
    this.updateErrorFlag();
  }
}
</script>
