import { shallowMount, Wrapper } from "@vue/test-utils";
import DefineAdvancedRow from "@/components/DefineAdvancedRow.vue";

const mountStandardRow = (values = {}) => {
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

const changeRowType = function<T extends Vue>(
  wrapper: Wrapper<T>,
  selector: string
): void {
  const measureButton = wrapper.find(selector);
  const measureElement = measureButton.element as HTMLInputElement;
  measureElement.checked = true;
  measureButton.trigger("change");
};

describe("DefineAdvancedRow", () => {
  describe("when changing a standard column to a measure column", () => {
    const wrapper = mountStandardRow();
    changeRowType(wrapper, "#columnTypeMeasure");

    it("Generates a sub-property for the measure", () => {
      expect(wrapper.props().value).toHaveProperty("measure");
    });

    it("Generates a resource URI for the measure using the pattern dataset-column-{_row}", () => {
      expect(wrapper.props().value.measure.valueUrl).toBe(
        "http://datadock.io/kal/test/id/resource/some_dataset-column_a-{_row}"
      );
    });

    it("Uses the row entity resource as the aboutUrl for the measure entity", () => {
      expect(wrapper.props().value.measure.aboutUrl).toBe(
        "http://datadock.io/kal/test/id/resource/dataset/{_row}"
      );
    });

    it("uses the column propertyUrl as the default measure property", () => {
      expect(wrapper.props().value.measure.propertyUrl).toBe(
        "http://datadock.io/kal/test/id/definition/column_a"
      );
    });

    it("moves the column title to the measure resource", () => {
      expect(wrapper.props().value.measure.titles[0]).toBe("Column A");
      expect(wrapper.props().value.titles.length).toBe(0);
    });
  });
});
