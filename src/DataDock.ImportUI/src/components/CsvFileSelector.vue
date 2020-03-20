<template>
  <div class="csv-file-selector">
    <div class="ui form">
      <div class="four fields">
        <div class="field">
          <label>Delimiter</label>
          <input type="text" placeholder="auto" v-model="csvDelimiter" />
        </div>
        <div class="field">
          <label>Line Endings</label>
          <div class="inline fields">
            <div class="field">
              <input
                type="radio"
                name="newline"
                value=""
                id="newline-auto"
                v-model="csvLineEnding"
              />
              <label for="newline-auto">Auto</label>
            </div>
            <div class="field">
              <input
                type="radio"
                name="newline"
                value="\n"
                id="newline-n"
                v-model="csvLineEnding"
              />
              <label for="newline-n">\n</label>
            </div>
            <div class="field">
              <input
                type="radio"
                name="newline"
                value="\r"
                id="newline-r"
                v-model="csvLineEnding"
              />
              <label for="newline-r">\r</label>
            </div>
            <div class="field">
              <input
                type="radio"
                name="newline"
                value="\r\n"
                id="newline-rn"
                v-model="csvLineEnding"
              />
              <label for="newline-rn">\r\n</label>
            </div>
          </div>
        </div>
        <div class="field">
          <label>Encoding</label>
          <input type="text" placeholder="default" v-model="csvEncoding" />
        </div>
        <div class="field">
          <label>Comment character:</label>
          <input type="text" placeholder="default" v-model="csvCommentChar" />
        </div>
      </div>
    </div>
    <div class="ui text container">
      <div class="ui fluid action input">
        <input
          id="fileSelectTextBox"
          readonly
          type="text"
          placeholder="Select CSV file for processing"
          v-on:click="selectFile"
          v-model="filePath"
        />
        <input
          id="fileSelect"
          type="file"
          accept=".csv"
          style="display:none"
          v-on:change="fileSelected"
        />
        <div class="ui icon button" v-on:click="selectFile">
          <i class="cloud upload icon" />
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { Component, Prop, Vue } from "vue-property-decorator";
import { parse, ParseConfig, ParseResult, ParseError } from "papaparse";

@Component
export default class CsvFileSelector extends Vue {
  filePath: string = "";
  errorMessage: string = "";
  csvDelimiter: string = "";
  csvEncoding: string = "";
  csvLineEnding: string = "";
  csvCommentChar: string = "";
  selectFile(event: any) {
    let fileSelect = document.getElementById("fileSelect") as HTMLElement;
    fileSelect.click();
  }

  fileSelected(event: Event) {
    let fileSelect = event.target as HTMLInputElement;
    let files = fileSelect.files;
    if (files != null) {
      this.filePath = (files[0] as File).name;
      let config: ParseConfig = {
        header: false,
        preview: 0,
        download: false,
        skipEmptyLines: true,
        complete: this.parseComplete,
        error: this.parseError
      };
      if (this.csvDelimiter) config.delimiter = this.csvDelimiter;
      if (this.csvCommentChar) config.comments = this.csvCommentChar;
      if (this.csvEncoding) config.encoding = this.csvEncoding;
      if (this.csvLineEnding) config.newline = this.csvLineEnding;
      parse(files[0], config);
    }
  }

  parseComplete(result: ParseResult, parsedFile: File | undefined) {
    this.$emit("csv-loaded", result.data, parsedFile);
  }

  parseError(error: ParseError, parsedFile: File | undefined) {
    this.errorMessage = error.message;
  }
}
</script>
