<template>
  <div>
    <div class="ui horizontal list">
      <div class="item" v-for="tag in tags" v-bind:key="tag">
        <div class="content">
          <div class="ui large label">
            {{ tag }}
            <i class="delete icon" v-on:click="removeTag(tag)"></i>
          </div>
        </div>
      </div>
    </div>
    <div class="field">
      <input
        type="text"
        v-on:blur="addTag()"
        v-on:keyup.enter="addTag()"
        v-model.trim="newTag"
        placeholder="Add a tag"
      />
    </div>
  </div>
</template>

<script lang="ts">
import { Component, Prop, Vue } from "vue-property-decorator";

@Component
export default class TagList extends Vue {
  @Prop() private tags!: string[];
  private newTag: string = "";

  addTag() {
    if (this.newTag) {
      this.tags.push(this.newTag);
      this.newTag = "";
    }
  }

  removeTag(tag: string) {
    let tagIx = this.tags.indexOf(tag);
    if (tagIx > 0) {
      this.tags.splice(tagIx, 1);
    }
  }
}
</script>
