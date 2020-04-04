<template>
  <tr>
    <td class="top aligned">
      {{ value.name }}
    </td>
    <td>
      <div class="inline fields">
        <label :for="value.name + '_columnType'">Column Type: </label>
        <div
          class="field"
          data-tooltip="Values in suppressed columns do not appear in the linked data."
        >
          <div class="ui radio checkbox">
            <input
              type="radio"
              id="columnTypeSuppressed"
              value="suppressed"
              :checked="value.columnType == 'suppressed'"
            />
            <label>Suppressed</label>
          </div>
        </div>
        <div
          class="field"
          data-tooltip="Values in standard columns are converted to properties of the row or parent column resource."
        >
          <div class="ui radio checkbox">
            <input
              type="radio"
              id="columnTypeStandard"
              value="standard"
              v-model="value.columnType"
            />
            <label>Standard</label>
          </div>
        </div>
        <div
          class="field"
          data-tooltip="Values in measure columns are converted to a child resource with a value property and optional fixed-value properties."
        >
          <div class="ui radio checkbox">
            <input
              type="radio"
              id="columnTypeMeasure"
              value="measure"
              v-model="value.columnType"
              @change="onColumnTypeChanged"
            />
            <label>Measure</label>
          </div>
        </div>
        <span
          data-tooltip="Determines how the column should be converted to linked data."
        >
          <i class="question circle icon" />
        </span>
      </div>
      <div v-if="value.columnType != 'measure'">
        <div
          v-if="!isVirtualColumn"
          class="ui field required"
          :class="{ error: !titleValid }"
        >
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
          <select
            :id="value.name + '_datatype'"
            v-model="value.datatype"
            @change="onDatatypeChanged"
          >
            <option value="string">Text</option>
            <option value="uri">URI</option>
            <option value="integer">Whole Number</option>
            <option value="decimal">Decimal Number</option>
            <option value="date">Date</option>
            <option value="dateTime">Date and Time</option>
            <option value="boolean" v-if="!isVirtualColumn">True/False</option>
            <option value="uriTemplate">URI Template</option>
          </select>
        </div>
        <div
          class="field"
          :class="{ error: !defaultValid }"
          v-if="
            isVirtualColumn &&
              value.datatype != 'uriTemplate' &&
              value.datatype != 'uri'
          "
        >
          <label :for="value.name + '_default'">Fixed Value</label>
          <input
            :id="value.name + '_default'"
            type="text"
            v-model="value.default"
          />
          <div class="ui error message" v-if="errors.default">
            {{ errors.default }}
          </div>
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
        <uri-template-input
          v-if="value.datatype == 'uriTemplate'"
          v-model="this.value.valueUrl"
          :name="value.name + '_uriTemplate'"
          label="URI Template String"
          :required="true"
          :allowStatic="false"
          :templateMetadata="templateMetadata"
          @error="onInputError"
        ></uri-template-input>
        <prefixed-uri-input
          label="Value URI"
          required="true"
          v-model="valueUrl"
          v-on:error="onValueUriInputError"
          v-if="isVirtualColumn && value.datatype == 'uri'"
        ></prefixed-uri-input>
        <div class="field" :class="{ error: !parentColumnValid }">
          <label :for="value.name + '_parent'">Parent Column</label>
          <select :id="value.name + '_parent'" v-model="parentColumn">
            <option value="_row"> [Row] </option>
            <option
              v-for="col in templateMetadata.tableSchema.columns"
              :key="'parent_' + col.name"
              :value="col.name"
              :disabled="col.datatype != 'uri' && col.datatype != 'uriTemplate'"
            >
              {{ col.name }}
            </option>
          </select>
        </div>
      </div>
      <div v-if="value.columnType == 'measure'">
        <h4 class="ui dividing header">Measure</h4>
        <prefixed-uri-input
          label="Property URI"
          required="true"
          v-model="value.measure.propertyUrl"
          v-on:error="onUriInputError"
        ></prefixed-uri-input>
        <uri-template-input
          :name="value.name + '_measure_valueUrl'"
          label="URI Template String"
          required="true"
          :templateMetadata="templateMetadata"
          v-model="value.measure.valueUrl"
          v-on:error="onInputError"
        ></uri-template-input>
        <h4 class="ui dividing header">Value</h4>
        <prefixed-uri-input
          :name="value.name + '_value_propertyUrl'"
          label="Property URI"
          required="true"
          v-model="value.propertyUrl"
          v-on:error="onUriInputError"
        ></prefixed-uri-input>
        <div class="field">
          <label :for="value.name + '_datatype'">Datatype</label>
          <select
            :id="value.name + '_datatype'"
            v-model="value.datatype"
            @change="onDatatypeChanged"
          >
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
        <h4 class="ui dividing header">Facets</h4>
        <div v-for="facet of value.facets" :key="facet.name">
          <prefixed-uri-input
            :name="facet.name + '_propertyUrl'"
            label="Facet Property URI"
            required="true"
            v-model="facet.propertyUrl"
            v-on:error="onInputError"
          ></prefixed-uri-input>
          <div class="field">
            <label :for="facet.name + '_datatype'">Datatype</label>
            <select :id="facet.name + '_datatype'" v-model="facet.datatype">
              <option value="string">Text</option>
              <option value="integer">Whole Number</option>
              <option value="decimal">Decimal Number</option>
              <option value="date">Date</option>
              <option value="dateTime">Date and Time</option>
              <option value="boolean">True/False</option>
            </select>
          </div>
          <div class="field">
            <label :for="facet.name + '_default'">Value</label>
            <select :id="facet.name + '_default'" v-model="facet.default">
              <option value="string">Text</option>
              <option value="uri">URI</option>
              <option value="integer">Whole Number</option>
              <option value="decimal">Decimal Number</option>
              <option value="date">Date</option>
              <option value="dateTime">Date and Time</option>
              <option value="boolean">True/False</option>
            </select>
          </div>
        </div>
        <button class="ui button">Add Facet</button>
      </div>
    </td>
  </tr>
</template>

<script lang="ts">
import Vue from "vue";
import { Component, Prop, Watch } from "vue-property-decorator";
import DefineColumnsRow from "@/components/DefineColumnsRow.vue";
import PrefixedUriInput from "@/components/PrefixedUriInput.vue";
import UriTemplateInput from "@/components/UriTemplateInput.vue";
import * as _ from "lodash";
import { Helper, SnifferOptions, Schema } from "@/DataDock";

@Component({
  components: {
    PrefixedUriInput,
    UriTemplateInput
  }
})
export default class DefineAdvancedRow extends Vue {
  @Prop() private value: any;
  @Prop() private colIx!: number;
  @Prop() private identifierBase!: string;
  @Prop() private resourceIdentifierBase!: string;
  @Prop() private templateMetadata: any;
  private uriTemplate: string =
    "valueUrl" in this.value ? this.value["valueUrl"] : "";
  private valueUrl: string =
    "valueUrl" in this.value ? this.value["valueUrl"] : "";
  //private columnType: string = this.getColumnType();
  private errors: any = {};
  private hasErrors: boolean | undefined;
  private _uriTemplateValid: boolean = true;
  private _validator = new SnifferOptions();

  private notifyChange() {
    this.$emit("input", this.value);
  }

  private get parentColumn(): string {
    if ("aboutUrl" in this.value) {
      var parentColumns = _.filter(
        this.templateMetadata.tableSchema.columns,
        c => {
          return "valueUrl" in c && c.valueUrl == this.value.aboutUrl;
        }
      );
      if (parentColumns.length == 0) {
        return "_row";
      }
      return parentColumns[0].name;
    } else {
      return "_row";
    }
  }

  private set parentColumn(newValue: string) {
    var parentColumns = _.filter(
      this.templateMetadata.tableSchema.columns,
      c => {
        return c.name === newValue && "valueUrl" in c;
      }
    );
    if (parentColumns.length == 0) {
      delete this.value.aboutUrl;
    } else {
      this.value.aboutUrl = parentColumns[0].valueUrl;
    }
  }

  private get parentColumnValid(): boolean {
    if ("aboutUrl" in this.value) {
      return _.some(this.templateMetadata.tableSchema.columns, c => {
        return (
          "valueUrl" in c &&
          (c.datatype == "uri" || c.datatype == "uriTemplate") &&
          c.valueUrl === this.value.aboutUrl
        );
      });
    } else {
      return true;
    }
  }

  private get isVirtualColumn(): boolean {
    return "virtual" in this.value && this.value.virtual;
  }

  @Watch("uriTemplate")
  onUriTemplateChanged() {
    this.validateUriTemplate();
    this.value.valueUrl = this.uriTemplate;
    this.notifyChange();
  }

  @Watch("valueUrl")
  onValueUrlChanged() {
    this.value.valueUrl = this.valueUrl;
    this.notifyChange();
  }

  private onDatatypeChanged() {
    if (this.value.datatype == "uriTemplate" && !("valueUrl" in this.value)) {
      this.value.valueUrl =
        this.identifierBase +
        "/" +
        this.value.name +
        "/{" +
        this.value.name +
        "}";
    }
    this.validateUriTemplate();
    this.notifyChange();
  }

  private onColumnTypeChanged() {
    if (!("measure" in this.value)) {
      // Create a new measure virtual column
      this.value.measure = {
        name: this.value.name + "_measure",
        propertyUrl: this.value.propertyUrl,
        aboutUrl: this.value.aboutUrl,
        valueUrl: this.value.propertyUrl + "-{_row}"
      };
      this.value.propertyUrl =
        "http://www.w3.org/1999/02/22-rdf-syntax-ns#value";
    }
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
    if (!this.isVirtualColumn) {
      if (this.value.titles[0].length == 0) {
        this.errors.title = "A non-empty title is required";
      }
    }
    this.updateErrorFlag();
    return !this.errors.title;
  }

  private get uriTemplateValid(): boolean {
    if (this.value.datatype === "uriTemplate") {
      this.validateUriTemplate();
      return this._uriTemplateValid;
    } else {
      delete this.errors.uriTemplate;
      return true;
    }
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

  private onUriInputError(isValid: boolean, errors: any) {
    if (isValid) {
      delete this.errors.propertyUrl;
    } else {
      this.errors.propertyUrl = errors["value"];
    }
    this.updateErrorFlag();
  }

  private onValueUriInputError(isValid: boolean, errors: any) {
    if (isValid) {
      delete this.errors.valueUrl;
    } else {
      this.errors.valueUrl = errors["value"];
    }
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

  private readonly templateRegex: RegExp = /{([^}]+)}/g;

  private hasColumn(colName: string) {
    for (let col of this.templateMetadata.tableSchema.columns) {
      if (col.name === colName) return true;
    }
    return false;
  }

  private validateUriTemplate() {
    delete this.errors.uriTemplate;
    if (this.value.datatype == "uriTemplate") {
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
          if (columnRef !== "_row" && !this.hasColumn(columnRef)) {
            this.errors.uriTemplate =
              "Template references a non-existant column with name " + match;
            this._uriTemplateValid = false;
          }
        }
      }
    }
    this.updateErrorFlag();
  }
}
</script>
