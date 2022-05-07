import {
  DatatypeSniffer,
  SnifferOptions,
  DatatypeEnum,
  Helper
} from "@/DataDock";

describe("DatatypeSniffer", () => {
  let datatypeSniffer = new DatatypeSniffer(new SnifferOptions());
  describe("sniffColumn", () => {
    it("sniffs boolean true/false", () => {
      const data = [["colA"], ["true"], ["false"], ["true"]];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Boolean);
    });
    it("sniffs boolean 0/1", function() {
      var data = [["colA"], ["0"], ["1"], ["0"]];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Boolean);
    });
    it("sniffs mixed boolean", function() {
      var data = [["colA"], ["true"], ["false"], ["0"], ["1"]];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Boolean);
    });

    it("sniffs http, https and ftp uris", function() {
      var data = [
        ["uri"],
        ["http://example.org"],
        ["https://datadock.io/foo/bar?baz=bletch#eep"],
        ["ftp://some.ftp.server/somewhere"]
      ];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Uri);
    });
    it("does not sniff generic urns as uris", function() {
      var data = [["urn"], ["urn:foo:bar"], ["isbn:0987654321"]];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.None);
    });
    it("sniffs integers", function() {
      var data = [["ints"], ["123"], ["+123"], ["-123"]];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Integer);
    });
    it("sniffs decimals", function() {
      var data = [["1.23"], ["-0.54"], ["+123.345"]];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Decimal);
    });
    it("sniffs decimals and integers as decimal", function() {
      var data = [
        ["mix"],
        ["1.23"],
        ["-0.54"],
        ["+123.345"],
        ["123"],
        ["+123"],
        ["-123"]
      ];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Decimal);
    });
    it("sniffs floats", function() {
      var data = [
        ["floats"],
        ["1.0E1"],
        ["-4E10"],
        ["0.234E-10"],
        ["INF"],
        ["-INF"],
        ["NaN"]
      ];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Float);
    });
    it("sniffs floats, decimals and integers as float", function() {
      var data = [
        ["mix"],
        ["1.0E1"],
        ["-4E10"],
        ["0.234E-10"],
        ["INF"],
        ["-INF"],
        ["NaN"],
        ["1.23"],
        ["-0.54"],
        ["+123.345"],
        ["123"],
        ["+123"],
        ["-123"]
      ];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Float);
    });
    it("sniffs UK Format Date Values", function() {
      let data = [
        ["dates"],
        ["23/12/2020"],
        ["23/12/20"],
        ["1/2/3"],
        ["23/02/-1000"]
      ];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Date);
    });
    it("sniffs US Format date values", function() {
      let data = [
        ["dates"],
        ["12/23/2020"],
        ["12/23/20"],
        ["1/2/3"],
        ["02/23/-1000"]
      ];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Date);
    });
    it("sniffs ISO format date values", function() {
      let data = [["dates"], ["2020-12-23"], ["1-2-3"], ["-1000-02-23"]];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Date);
    });
    it("sniffs ISO date/time values with no timezone or fractional seconds", function() {
      const data = [
        ["datetimes"],
        ["2020-12-23T02:01:00"],
        ["-2020-12-23T02:01:00"]
      ];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.DateTime);
    });
    it("sniffs ISO date/time values with timezone and no fractional seconds", function() {
      const data = [
        ["datetimes"],
        ["2020-12-23T02:01:00Z"],
        ["2020-12-23T02:01:00-11:00"],
        ["2020-12-23T02:01:00+09:00"]
      ];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.DateTime);
    });
    it("sniffs ISO date/time values with no timezone and fractional seconds", function() {
      const data = [
        ["datetimes"],
        ["2020-12-23T02:01:00.1"],
        ["2020-12-23T02:01:00.01"]
      ];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.DateTime);
    });
    it("sniffs ISO date/time values with timezones and fractional seconds", function() {
      const data = [
        ["datetimes"],
        ["2020-12-23T02:01:00.1Z"],
        ["2020-12-23T02:01:00.12-11:00"],
        ["2020-12-23T02:01:00.123+09:00"]
      ];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.DateTime);
    });
    it("sniffs mixed date and datetime as string", function() {
      var data = [
        ["mix"],
        ["23/12/2020"],
        ["23/12/20"],
        ["12/23/2020"],
        ["23/12/2020"],
        ["2020-12-23"],
        ["2020-12-23T02:01:00Z"],
        ["2020-12-23T02:01:00-11:00"],
        ["2020-12-23T02:01:00+09:00"],
        ["2020-12-23T02:01:00.1"],
        ["2020-12-23T02:01:00.01"]
      ];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.None);
    });
  });
});

describe("Helper", () => {
  describe("makeTemplate", () => {
    it("removes the datatype property from a uriTemplate column", () => {
      var templateViewModel = {
        tableSchema: {
          columns: [
            {
              name: "colA",
              propertyUrl: "http://example.org/p",
              datatype: "uriTemplate",
              valueUrl: "http://example.org/id/{colA}"
            }
          ]
        }
      };
      var template = Helper.makeTemplate(templateViewModel);
      var sanitizedColA = template.tableSchema.columns[0];
      expect(sanitizedColA).not.toHaveProperty("datatype");
      expect(sanitizedColA).toHaveProperty(
        "valueUrl",
        "http://example.org/id/{colA}"
      );
    });

    it("replaces the datatype property for a uri column with a valueUrl property", () => {
      var templateViewModel = {
        tableSchema: {
          columns: [
            {
              name: "colA",
              propertyUrl: "http://example.org/p",
              datatype: "uri"
            }
          ]
        }
      };
      var template = Helper.makeTemplate(templateViewModel);
      var sanitizedColA = template.tableSchema.columns[0];
      expect(sanitizedColA).not.toHaveProperty("datatype");
      expect(sanitizedColA).toHaveProperty("valueUrl", "{colA}");
    });

    it("does not modify a string column", () => {
      var templateViewModel = {
        tableSchema: {
          columns: [
            {
              name: "colA",
              propertyUrl: "http://example.org/p",
              datatype: "string"
            }
          ]
        }
      };
      var template = Helper.makeTemplate(templateViewModel);
      var sanitizedColA = template.tableSchema.columns[0];
      expect(sanitizedColA).toHaveProperty("datatype");
      expect(sanitizedColA.datatype).toBe("string");
      expect(sanitizedColA).not.toHaveProperty("valueUrl");
    });
  });

  describe("makeTemplateViewModel", () => {
    it("Adds a uri datatype when the valueUrl is only a column reference", () => {
      var template = {
        tableSchema: {
          columns: [
            {
              name: "colA",
              propertyUrl: "http://example.org/p",
              valueUrl: "{colA}"
            }
          ]
        }
      };
      var templateViewModel = Helper.makeTemplateViewModel(template);
      expect(templateViewModel).toHaveProperty("tableSchema");
      expect(templateViewModel.tableSchema).toHaveProperty("columns");
      expect(templateViewModel.tableSchema.columns).toHaveLength(1);
      var col = templateViewModel.tableSchema.columns[0];
      expect(col).toHaveProperty("datatype");
      expect(col.datatype).toBe("uri");
      expect(col.valueUrl).toBe("{colA}");
    });

    it("Adds a uriTemplate datatype when the valueUrl is not only a column reference", () => {
      var template = {
        tableSchema: {
          columns: [
            {
              name: "colA",
              propertyUrl: "http://example.org/p",
              valueUrl: "http://example.org/id/{colA}"
            }
          ]
        }
      };
      var templateViewModel = Helper.makeTemplateViewModel(template);
      var col = templateViewModel.tableSchema.columns[0];
      expect(col).toHaveProperty("datatype");
      expect(col.datatype).toBe("uriTemplate");
      expect(col.valueUrl).toBe("http://example.org/id/{colA}");
    });

    it("Adds columnType=suppressed when suppressOutput is true on a column schema", () => {
      var template = {
        tableSchema: {
          columns: [
            {
              name: "colA",
              suppressOutput: true
            }
          ]
        }
      };
      var templateViewModel = Helper.makeTemplateViewModel(template);
      var col = templateViewModel.tableSchema.columns[0];
      expect(col).toHaveProperty("columnType", "suppressed");
    });

    it("Adds columnType=measure when dd:columnType=measure on a column schema", () => {
      var template = {
        tableSchema: {
          columns: [
            {
              name: "colA",
              "http://schema.datadock.io/columnType": "measure"
            }
          ]
        }
      };
      var templateViewModel = Helper.makeTemplateViewModel(template);
      var col = templateViewModel.tableSchema.columns[0];
      expect(col).toHaveProperty("columnType", "measure");
    });

    it("Marks measure facet and parent virutal columns as hidden", () => {
      var template = {
        tableSchema: {
          columns: [
            {
              name: "colA",
              datatype: "string",
              "http://schema.datadock.io/columnType": "measure",
              aboutUrl: "http://datadock.io/kal/test/id/resource/book/{colA}",
              propertyUrl: "http://datadock.io/kal/id/definition/sales",
              "http://schema.datadock.io/facetColumn": ["colC"]
            },
            {
              name: "colB",
              virtual: "true",
              propertyUrl:
                "http://datadock.io/kal/test/id/definition/someProperty",
              valueUrl: "http://datadock.io/kal/test/id/resource/book/{colA}"
            },
            {
              name: "colC",
              virtual: "true",
              propertyUrl: "http://datadock.io/kal/test/id/definition/year",
              default: "2019",
              aboutUrl: "http://datadock.io/kal/test/id/resource/book/{colA}"
            },
            {
              name: "colD",
              virtual: "true",
              propertyUrl:
                "http://datadock.io/kal/test/id/definition/customSubProperty",
              default: "whoa!",
              aboutUrl: "http://datadock.io/kal/test/id/resource/book/{colA}"
            }
          ]
        }
      };
      var templateViewModel = Helper.makeTemplateViewModel(template);
      expect(templateViewModel.tableSchema.columns).toHaveLength(2);
      var colA = templateViewModel.tableSchema.columns[0];
      expect(colA).toHaveProperty("measure");
      expect(colA.measure).toHaveProperty("name", "colB");
      expect(colA.measure).toHaveProperty("hidden", true);
      expect(colA).toHaveProperty("facets");
      expect(colA.facets).toHaveLength(1);
      expect(colA.facets[0]).toHaveProperty("name", "colC");
      expect(colA.facets[0]).toHaveProperty("hidden", true);
      var colD = templateViewModel.tableSchema.columns[1];
      expect(colD).toHaveProperty("name", "colD");
      expect(colD).not.toHaveProperty("hidden");
    });

    it("Collects parent column information for measure columns", () => {
      var template = {
        tableSchema: {
          aboutUrl: "http://datadock.io/kal/test/id/resource/isbn/{isbn}",
          columns: [
            {
              name: "isbn",
              datatype: "string",
              propertyUrl: "http://datadock.io/kal/test/id/definition/isbn"
            },
            {
              name: "sales_2020",
              datatype: "integer",
              titles: ["2020 Sales"],
              "http://schema.datadock.io/columnType": "measure",
              "http://schema.datadock.io/facetColumn": ["virtualCol2"],
              aboutUrl:
                "http://datadock.io/kal/test/id/resource/sales_2020/{isbn}",
              propertyUrl: "http://www.w3.org/1999/02/22-rdf-syntax-ns#value"
            },
            {
              name: "virtualCol1",
              virtual: true,
              titles: ["Sales By Year"],
              propertyUrl: "http://datadock.io/kal/test/id/definition/sales",
              valueUrl:
                "http://datadock.io/kal/test/id/resource/sales_2020/{isbn}"
            },
            {
              name: "virtualCol2",
              virtual: true,
              aboutUrl:
                "http://datadock.io/kal/test/id/resource/sales_2020/{isbn}",
              propertyUrl: "http://datadock.io/kal/test/id/definition/year",
              default: "2020",
              datatype: "integer",
              titles: ["Year"]
            }
          ]
        }
      };

      var expected = {
        tableSchema: {
          aboutUrl: "http://datadock.io/kal/test/id/resource/isbn/{isbn}",
          columns: [
            {
              name: "isbn",
              datatype: "string",
              columnType: "standard",
              propertyUrl: "http://datadock.io/kal/test/id/definition/isbn"
            },
            {
              name: "sales_2020",
              columnType: "measure",
              aboutUrl:
                "http://datadock.io/kal/test/id/resource/sales_2020/{isbn}",
              propertyUrl: "http://www.w3.org/1999/02/22-rdf-syntax-ns#value",
              "http://schema.datadock.io/columnType": "measure",
              "http://schema.datadock.io/facetColumn": ["virtualCol2"],
              titles: ["2020 Sales"],
              datatype: "integer",
              measure: {
                name: "virtualCol1",
                titles: ["Sales By Year"],
                virtual: true,
                hidden: true,
                datatype: "uriTemplate",
                propertyUrl: "http://datadock.io/kal/test/id/definition/sales",
                valueUrl:
                  "http://datadock.io/kal/test/id/resource/sales_2020/{isbn}"
              },
              facets: [
                {
                  name: "virtualCol2",
                  columnType: "standard",
                  virtual: true,
                  hidden: true,
                  titles: ["Year"],
                  aboutUrl:
                    "http://datadock.io/kal/test/id/resource/sales_2020/{isbn}",
                  propertyUrl: "http://datadock.io/kal/test/id/definition/year",
                  default: "2020",
                  datatype: "integer"
                }
              ]
            }
          ]
        }
      };

      var templateViewModel = Helper.makeTemplateViewModel(template);
      expect(templateViewModel).toMatchObject(expected);

      var roundTripModel = Helper.makeTemplate(templateViewModel);
      expect(roundTripModel).toMatchObject(template);
    });
    it("annotates new measure columns correctly", () => {
      var templateViewModel = {
        tableSchema: {
          columns: [
            {
              name: "colA",
              columnType: "measure",
              propertyUrl: "http://www.w3.org/1999/02/22-rdf-syntax-ns#value",
              datatype: "integer",
              measure: {
                name: "colA_measure",
                titles: ["Some Title"],
                propertyUrl: "http://datadock.io/kal/test/id/definition/colA",
                valueUrl: "http://datadock.io/kal/test/id/resource/colA-{_row}"
              }
            }
          ]
        }
      };
      var expectedTemplate = {
        tableSchema: {
          columns: [
            {
              name: "colA",
              "http://schema.datadock.io/columnType": "measure",
              aboutUrl: "http://datadock.io/kal/test/id/resource/colA-{_row}",
              propertyUrl: "http://www.w3.org/1999/02/22-rdf-syntax-ns#value",
              datatype: "integer"
            },
            {
              name: "colA_measure",
              titles: ["Some Title"],
              propertyUrl: "http://datadock.io/kal/test/id/definition/colA",
              valueUrl: "http://datadock.io/kal/test/id/resource/colA-{_row}",
              virtual: true
            }
          ]
        }
      };

      var template = Helper.makeTemplate(templateViewModel);
      expect(template).toMatchObject(expectedTemplate);
    });
  });
});
