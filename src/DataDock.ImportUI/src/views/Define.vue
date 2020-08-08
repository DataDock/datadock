<template>
  <div class="ui attached segment">
    <div class="ui error message" v-if="csvFile == null">
      Please select a CSV file to upload to start
    </div>
    <div class="ui two column stackable grid container" v-if="csvFile != null">
      <div class="four wide column">
        <div class="ui vertical steps">
          <div
            @click.prevent="setActive('details')"
            :class="{ active: isActive('details'), step: true }"
          >
            <i
              class="circular inverted red exclamation icon"
              style="font-size: 0.75em"
              v-if="hasError('details')"
            >
            </i>
            <div class="content">
              Dataset Details
            </div>
          </div>
          <div
            @click.prevent="setActive('identifiers')"
            :class="{ active: isActive('identifiers'), step: true }"
          >
            <i
              class="circular inverted red exclamation icon"
              style="font-size: 0.75em"
              v-if="hasError('identifiers')"
            >
            </i>
            <div class="content">
              Identifiers
            </div>
          </div>
          <div
            @click.prevent="setActive('definitions')"
            :class="{ active: isActive('definitions'), step: true }"
          >
            <i
              class="circular inverted red exclamation icon"
              style="font-size: 0.75em"
              v-if="hasError('definitions')"
            >
            </i>
            <div class="content">
              Column Definitions
            </div>
          </div>
          <div
            @click.prevent="setActive('advanced')"
            :class="{ active: isActive('advanced'), step: true }"
          >
            <i
              class="circular inverted red exclamation icon"
              style="font-size: 0.75em"
              v-if="hasError('advanced')"
            >
            </i>
            <div class="content">
              Advanced
            </div>
          </div>
          <div
            @click.prevent="setActive('preview')"
            :class="{ active: isActive('preview'), step: true }"
          >
            <div class="content">
              Data Preview
            </div>
          </div>
          <div
            @click.prevent="setActive('templatePreview')"
            :class="{ active: isActive('templatePreview'), step: true }"
          >
            <div class="content">
              Template Preview
            </div>
          </div>
        </div>
      </div>
      <div class="twelve wide stretched column">
        <div v-show="isActive('details')">
          <define-details
            v-model="templateMetadata"
            :identifierBase="identifierBase"
            @titleChanged="onTitleChanged"
            @error="onError('details', $event)"
          ></define-details>
        </div>
        <div v-show="isActive('identifiers')">
          <define-identifiers
            v-model="templateMetadata"
            :identifierBase="identifierBase"
            :datasetId="datasetId"
          ></define-identifiers>
        </div>
        <div v-show="isActive('definitions')">
          <define-columns
            v-model="templateMetadata"
            :identifierBase="identifierBase"
            :resourceIdentifierBase="resourceIdentifierBase()"
            @error="onError('definitions', $event)"
          ></define-columns>
        </div>
        <div v-show="isActive('advanced')">
          <define-advanced
            v-model="templateMetadata"
            :identifierBase="identifierBase"
            :datasetId="datasetId"
            @error="onError('advanced', $event)"
          ></define-advanced>
        </div>
        <div v-show="isActive('preview')">
          <define-preview
            :templateMetadata="templateMetadata"
            :data="csvData"
            :pageSize="50"
          ></define-preview>
        </div>
        <div v-show="isActive('templatePreview')">
          <define-template v-model="templateMetadata"></define-template>
        </div>
      </div>
    </div>
  </div>
</template>
<script lang="ts">
import { Component, Prop, Vue, Watch } from "vue-property-decorator";
import DefineDetails from "@/components/DefineDetails.vue";
import DefineIdentifiers from "@/components/DefineIdentifiers.vue";
import DefineColumns from "@/components/DefineColumns.vue";
import DefineAdvanced from "@/components/DefineAdvanced.vue";
import DefinePreview from "@/components/DefinePreview.vue";
import DefineTemplate from "@/components/DefineTemplate.vue";
import { Helper, DatatypeSniffer, SnifferOptions } from "@/DataDock";

@Component({
  components: {
    DefineDetails,
    DefineIdentifiers,
    DefineColumns,
    DefineAdvanced,
    DefinePreview,
    DefineTemplate
  }
})
export default class Define extends Vue {
  @Prop() public templateMetadata: any;
  @Prop() public publishUrl!: string;
  @Prop() public ownerId!: string;
  @Prop() public repoId!: string;
  @Prop() public csvData!: string[][];
  @Prop() public csvFile?: File;

  // Data
  public activeItem: string = "details";
  public errorItems: string[] = [];
  public identifierBase: string =
    this.publishUrl + "/" + this.ownerId + "/" + this.repoId + "/id";
  public datasetId: string = Helper.slugify(this.templateMetadata["dc:title"]);

  // Computed
  public resourceIdentifierBase(): string {
    return this.$data.identifierBase + "/resource/" + this.$data.datasetId;
  }

  // Methods
  private isActive(menuItem: string) {
    return this.$data.activeItem === menuItem;
  }

  private setActive(menuItem: string) {
    this.$data.activeItem = menuItem;
  }

  private hasError(menuItem: string) {
    return this.$data.errorItems.includes(menuItem);
  }

  private onTitleChanged() {
    this.$data.datasetId = Helper.slugify(this.templateMetadata["dc:title"]);
  }

  private onError(panel: string, errorFlag: boolean) {
    if (errorFlag) {
      if (!this.$data.errorItems.includes(panel)) {
        this.$data.errorItems.push(panel);
      }
    } else {
      if (this.$data.errorItems.includes(panel)) {
        this.$data.errorItems.splice(this.$data.errorItems.indexOf(panel));
      }
    }
  }
}
</script>
