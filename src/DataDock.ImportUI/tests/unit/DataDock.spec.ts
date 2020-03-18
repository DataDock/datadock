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
    it("sniffs date values", function() {
      var data = [
        ["dates"],
        ["23/12/2020"],
        ["23/12/20"],
        ["12/23/2020"],
        ["23/12/2020"],
        ["2020-12-23"],
        ["-0163-05-13"]
      ];
      let colInfo = datatypeSniffer.sniffColumn(0, data);
      expect(colInfo.hasEmptyValues).toBe(false);
      expect(colInfo.allEmptyValues).toBe(false);
      expect(colInfo.datatype).toBe(DatatypeEnum.Date);
    });
    it("sniffs date/time values", function() {
      var data = [
        ["datetimes"],
        ["2020-12-23T02:01:00Z"],
        ["2020-12-23T02:01:00-11:00"],
        ["2020-12-23T02:01:00+09:00"],
        ["2020-12-23T02:01:00.1"],
        ["2020-12-23T02:01:00.01"]
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
  describe("makeCleanTemplate", () => {
    it("removes the datatype property from a uriTemplate column", () => {
      var template = {
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
      var sanitizedTemplate = Helper.makeCleanTemplate(template);
      var sanitizedColA = sanitizedTemplate.tableSchema.columns[0];
      expect(sanitizedColA).not.toHaveProperty("datatype");
      expect(sanitizedColA).toHaveProperty("valueUrl");
    });
    it("replaces the datatype property for a uri column with a valueUrl property", () => {
      var template = {
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
      var sanitizedTemplate = Helper.makeCleanTemplate(template);
      var sanitizedColA = sanitizedTemplate.tableSchema.columns[0];
      expect(sanitizedColA).not.toHaveProperty("datatype");
      expect(sanitizedColA).toHaveProperty("valueUrl");
      expect(sanitizedColA.valueUrl).toBe("{colA}");
    });
    it("does not modify a string column", () => {
      var template = {
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
      var sanitizedTemplate = Helper.makeCleanTemplate(template);
      var sanitizedColA = sanitizedTemplate.tableSchema.columns[0];
      expect(sanitizedColA).toHaveProperty("datatype");
      expect(sanitizedColA.datatype).toBe("string");
      expect(sanitizedColA).not.toHaveProperty("valueUrl");
    });
  });
});
