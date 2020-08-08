import { shallowMount } from "@vue/test-utils";
import DefineAdvancedRow from "@/components/DefineAdvancedRow.vue";

const factory = (values = {}) => {
  return shallowMount(DefineAdvancedRow, {
    propsData: {
      ...values,
      value: {
        name: "column_a",
        titles: ["Column A"],
        aboutUrl: "http://datadock.io/kal/test/id/resource/dataset/{_row}",
        propertyUrl: "http://datadock.io/kal/test/id/definition/column_a"
      },
      colIx: 0,
      identifierBase: "http://datadock.io/kal/test/id",
      datasetId: "some_dataset",
      templateMetadata: {
        tableSchema: {
          columns: []
        }
      }
    }
  });
};

describe("DefineAdvancedRow", () => {
  describe("when changing a standard column to a measure column", () => {
    const wrapper = factory();
    const measureButton = wrapper.find("#columnTypeMeasure");
    const measureElement = measureButton.element as HTMLInputElement;
    measureElement.checked = true;
    measureButton.trigger("change");

    it("Generates a resource URI for the measure", () => {
      expect(wrapper.props().value.measure.valueUrl).toBe(
        "http://datadock.io/kal/test/id/resource/some_dataset-column_a-{_row}"
      );
    });
  });
});
