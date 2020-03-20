<template>
  <form class="ui form" v-bind:class="{ error: hasErrors }">
    <div class="field" v-bind:class="{ error: !titleValid }">
      <label>Title</label>
      <input
        type="text"
        name="title"
        placeholder="Dataset Title"
        v-model.trim="title"
        @input="validateTitle"
      />
      <div class="ui error message" v-if="errors.title">{{ errors.title }}</div>
    </div>
    <div class="field">
      <label>Description</label>
      <textarea
        name="description"
        placeholder="Dataset description"
        v-model="value['dc:description']"
        @input="$emit('input', value)"
      />
    </div>
    <div class="field" v-bind:class="{ error: !licenseValid }">
      <label>Licenses</label>
      <select
        class="ui dropdown"
        v-model="value['dc:license']"
        aria-placeholder="Please select a license"
        @change="validateLicense"
      >
        <option disabled value="">Please select a license</option>
        <option value="https://creativecommons.org/publicdomain/zero/1.0/">
          Public Domain (CC-0)
        </option>
        <option value="https://creativecommons.org/licenses/by/4.0/">
          Attribution (CC-BY)
        </option>
        <option value="https://creativecommons.org/licenses/by-sa/4.0/">
          Attribution-ShareAlike (CC-BY-SA)
        </option>
        <option
          value="http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/"
        >
          Open Government License (OGL)
        </option>
        <option value="https://opendatacommons.org/licenses/pddl/">
          Open Data Commons Public Domain Dedication and License (PDDL)
        </option>
        <option value="https://opendatacommons.org/licenses/by/">
          Open Data Commons Attribution License (ODC-By)
        </option>
      </select>
      <div class="ui error message" v-if="errors.license">
        {{ errors.license }}
      </div>
    </div>
    <div class="field">
      <label>Tags</label>
      <TagList
        :tags="value['dcat:keyword']"
        @input="$emit('input', value)"
      ></TagList>
    </div>
  </form>
</template>

<script lang="ts">
import { Component, Prop, Vue, Watch } from "vue-property-decorator";
import TagList from "@/components/TagList.vue";
import { Helper } from "@/DataDock";

@Component({
  components: {
    TagList
  }
})
export default class DefineDetails extends Vue {
  @Prop() private value: any;
  @Prop() private identifierBase!: string;
  private title: string = this.value["dc:title"];
  private hasErrors: boolean = false;
  private errors: any = {};
  private titleValid: boolean = true;
  private licenseValid: boolean = true;

  @Watch("title") private onTitleChanged() {
    this.value["dc:title"] = this.title;
    const slug = Helper.slugify(this.title);
    this.value["url"] = this.identifierBase + "/dataset/" + slug;
    this.$emit("titleChanged");
    this.$emit("input", this.value);
  }

  private beforeMount() {
    this.validateTitle();
    this.validateLicense();
    this.updateErrorFlag();
  }

  private validateTitle() {
    delete this.errors["title"];
    if (this.title === undefined || this.title.length == 0) {
      this.errors["title"] = "A non-empty title is required";
      this.titleValid = false;
    } else {
      this.titleValid = true;
    }
    this.updateErrorFlag();
  }

  private validateLicense() {
    delete this.errors["license"];
    if (!this.value["dc:license"]) {
      this.errors["license"] = "Please select a license";
      this.licenseValid = false;
    } else {
      this.licenseValid = true;
    }
    this.updateErrorFlag();
  }

  private updateErrorFlag() {
    const hadErrors = this.hasErrors;
    this.hasErrors = false;
    for (let k in this.errors) {
      this.hasErrors = true;
      break;
    }
    if (this.hasErrors != hadErrors) {
      this.$emit("error", this.hasErrors);
    }
  }
}
</script>
