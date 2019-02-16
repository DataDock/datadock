describe("schema helper", function () {
    var metadata = {
        "dc:title": "the title",
        "dc:description": "a description",
        "dc:license": "http://an.example/license",
        "dcat:keyword": ["one", "two", "and three"]
    };
    var schema = {
        "tableSchema": {
            "columns": [
                {
                    "name": "name",
                    "titles": ["Name", "Nom"],
                    "propertyUrl": "http://xmlns.com/foaf/0.1/name",
                    "datatype": "http://www.w3.org/2000/10/XMLSchema#string",
                    "suppressOutput": true
                },
                {
                    "name": "age",
                    "titles": "Age"
                },
                {
                    "name": "email"
                }
            ]
        }
    }
    describe("getDatasetId", function () {
        it(
            "returns the specified property value if it exists",
            function() { expect(schemaHelper.getDatasetId({ "url": "bar" }, "baz")).toBe("bar"); }
        );
        it(
            "returns the default value if the property does not exist",
            function () { expect(schemaHelper.getDatasetId({ "uri": "bar" }, "baz")).toBe("baz"); }
        );
        it("returns the default value if the object is null",
            function () { expect(schemaHelper.getDatasetId(null, "baz")).toBe("baz"); }
        );
        it("returns the default value if the object is undefined",
            function () { expect(schemaHelper.getDatasetId(undefined, "baz")).toBe("baz"); }
        );
    });
    describe("getTitle",
        function() {
            it("returns dc:title", function() { expect(schemaHelper.getTitle(metadata, "")).toBe("the title") });
        });
    describe("getDescription",
        function() {
            it("returns dc:description",
                function() { expect(schemaHelper.getDescription(metadata, "")).toBe("a description") });
        });
    describe("getLicenseUri",
        function() {
            it("returns dc:license",
                function() { expect(schemaHelper.getLicenseUri(metadata, "")).toBe("http://an.example/license") });
        });
    describe("getTags",
        function() {
            it("returns dcat:keyword joined by commas",
                function() {
                    expect(schemaHelper.getTags(metadata)).toBe("one,two,and three");
                });
            it("returns an empty string as the default value",
                function() {
                    expect(schemaHelper.getTags({ "dc:title": "no tags here" })).toBe("");
                });
        });
    describe("getColumnTemplate",
        function() {
            it("returns default value if the metadata has no tableSchema",
                function() {
                    expect(schemaHelper.getColumnTemplate({}, "name")).toEqual({});
                });
            it("returns default value if the table schema has no columns",
                function() {
                    expect(schemaHelper.getColumnTemplate({ "tableSchema": { "no_columns": [] } })).toEqual({});
                });
            it("returns default value if the specified column is not found",
                function() {
                    expect(schemaHelper.getColumnTemplate(schema, "address")).toEqual({});
                });
            it("returns a named column that exists in the table schema",
                function() {
                    expect(schemaHelper.getColumnTemplate(schema, "age")).toEqual(
                        jasmine.objectContaining({ "name": "age" }));
                });
        });
    describe("getColumnTitle",
        function() {
            it("returns the first title if titles is an array",
                function() {
                    var col = schemaHelper.getColumnTemplate(schema, "name");
                    expect(schemaHelper.getColumnTitle(col, "")).toBe("Name");
                });
            it("returns the value of titles if titles is not an array",
                function() {
                    var col = schemaHelper.getColumnTemplate(schema, "age");
                    expect(schemaHelper.getColumnTitle(col, "")).toBe("Age");
                });
            it("returns the default value if there is no titles property",
                function() {
                    var col = schemaHelper.getColumnTemplate(schema, "email");
                    expect(schemaHelper.getColumnTitle(col, "")).toBe("");
                });
        });
    describe("getColumnPropertyUrl",
        function() {
            it("returns propertyUrl",
                function() {
                    var col = schemaHelper.getColumnTemplate(schema, "name");
                    expect(schemaHelper.getColumnPropertyUrl(col, "")).toBe("http://xmlns.com/foaf/0.1/name");
                });
            it("return default value if no propertyUrl",
                function() {
                    var col = schemaHelper.getColumnTemplate(schema, "email");
                    expect(schemaHelper.getColumnPropertyUrl(col, "")).toBe("");
                });
        });
    describe("getColumnDatatype",
        function() {
            it("returns datatype",
                function() {
                    var col = schemaHelper.getColumnTemplate(schema, "name");
                    expect(schemaHelper.getColumnDatatype(col, "")).toBe("http://www.w3.org/2000/10/XMLSchema#string");
                });
        });
    describe("getColumnSuppressed",
        function() {
            it("returns suppressOutput",
                function() {
                    var col = schemaHelper.getColumnTemplate(schema, "name");
                    expect(schemaHelper.getColumnSuppressed(col)).toBe(true);
                });
            it("defaults to false",
                function() {
                    var col = schemaHelper.getColumnTemplate(schema, "age");
                    expect(schemaHelper.getColumnSuppressed(col)).toBe(false);
                });
        });

    describe("makeAbsolute",
        function() {
            it("updates a top level aboutUrl property",
                function() {
                    var schema = { "aboutUrl": "some/relative/path" };
                    schemaHelper.makeAbsolute(schema, "http://datadock.io/foo/bar/");
                    expect(schema.aboutUrl).toBe("http://datadock.io/foo/bar/some/relative/path");
                });
            it("updates a nested aboutUrl property",
                function() {
                    var schema = { "metadata": { "aboutUrl": "some/relative/path" } };
                    schemaHelper.makeAbsolute(schema, "http://datadock.io/foo/bar/");
                    expect(schema.metadata.aboutUrl).toBe("http://datadock.io/foo/bar/some/relative/path");
                });
            it("updates a propertyUrl in a nested array",
                function() {
                    var schema = {
                        "metadata": {
                            "tableSchema": {
                                "columns": [
                                    {
                                        "name": "colName",
                                        "propertyUrl": "id/definition/colName",
                                        "datatype": "string"
                                    }
                                ]
                            }
                        }
                    }
                    schemaHelper.makeAbsolute(schema, "http://datadock.io/foo/bar/");
                    expect(schema.metadata.tableSchema.columns[0].propertyUrl)
                        .toBe("http://datadock.io/foo/bar/id/definition/colName");
                });
            it("updates a valueUrl in a nested array and does not update an absolute propertyUrl",
                function() {
                    var schema = {
                        "metadata": {
                            "tableSchema": {
                                "columns": [
                                    {
                                        "name": "colName",
                                        "propertyUrl": "http://example.org/foo",
                                        "valueUrl": "id/resource/widget/{colName}"
                                    }
                                ]
                            }
                        }
                    }
                    schemaHelper.makeAbsolute(schema, "http://datadock.io/foo/bar/");
                    expect(schema.metadata.tableSchema.columns[0].propertyUrl)
                        .toBe("http://example.org/foo");
                    expect(schema.metadata.tableSchema.columns[0].valueUrl)
                        .toBe("http://datadock.io/foo/bar/id/resource/widget/{colName}");
                });
        });
});