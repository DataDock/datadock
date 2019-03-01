describe("datatype sniffer", function() {
    describe("getDatatypes",
        function() {
            it("sniffs boolean true/false",
                function() {
                    var data = [
                        ["colA"],
                        ["true"],
                        ["false"],
                        ["true"]
                    ];
                    expect(datatypeSniffer.getDatatypes(data))
                        .toEqual([{ hasEmptyValues: false, allEmptyValues: false, type: "boolean" }]);
                });
            it("sniffs boolean 0/1",
                function() {
                    var data = [
                        ["colA"],
                        ["0"],
                        ["1"],
                        ["0"]
                    ];
                    expect(datatypeSniffer.getDatatypes(data))
                        .toEqual([{ hasEmptyValues: false, allEmptyValues: false, type: "boolean" }]);
                });
            it("sniffs mixed boolean",
                function() {
                    var data = [
                        ["colA"],
                        ["true"],
                        ["false"],
                        ["0"],
                        ["1"]
                    ];
                    expect(datatypeSniffer.getDatatypes(data)).toEqual([
                        { hasEmptyValues: false, allEmptyValues: false, type: "boolean" }
                    ]);
                });

            it("sniffs http, https and ftp uris",
                function() {
                    var data = [
                        ["uri"],
                        ["http://example.org"],
                        ["https://datadock.io/foo/bar?baz=bletch#eep"],
                        ["ftp://some.ftp.server/somewhere"]
                    ];
                    expect(datatypeSniffer.getDatatypes(data)).toEqual([
                        { hasEmptyValues: false, allEmptyValues: false, type: "uri" }
                    ]);
                });
            it("does not sniff generic urns as uris",
                function() {
                    var data = [
                        ["urn"],
                        ["urn:foo:bar"],
                        ["isbn:0987654321"]
                    ];
                    expect(datatypeSniffer.getDatatypes(data))
                        .toEqual([{ hasEmptyValues: false, allEmptyValues: false, type: "string" }]);
                });
            it("sniffs integers", function() {
                var data = [
                    ["ints"],
                    ["123"],
                    ["+123"],
                    ["-123"]
                ];
                expect(datatypeSniffer.getDatatypes(data))
                    .toEqual([{ hasEmptyValues: false, allEmptyValues: false, type: "integer" }]);
            });
            it("sniffs decimals",
                function() {
                    var data = [
                        ["1.23"],
                        ["-0.54"],
                        ["+123.345"]
                    ];
                    expect(datatypeSniffer.getDatatypes(data))
                        .toEqual([{ hasEmptyValues: false, allEmptyValues: false, type: "decimal" }]);
                });
            it("sniffs decimals and integers as decimal",
                function () {
                    var data = [
                        ["mix"],
                        ["1.23"],
                        ["-0.54"],
                        ["+123.345"],
                        ["123"],
                        ["+123"],
                        ["-123"]
                    ];
                    expect(datatypeSniffer.getDatatypes(data))
                        .toEqual([{ hasEmptyValues: false, allEmptyValues: false, type: "decimal" }]);
                });
            it("sniffs floats",
                function() {
                    var data = [
                        ["floats"],
                        ["1.0E1"],
                        ["-4E10"],
                        ["0.234E-10"],
                        ["INF"],
                        ["-INF"],
                        ["NaN"]
                    ];
                    expect(datatypeSniffer.getDatatypes(data))
                        .toEqual([{ hasEmptyValues: false, allEmptyValues: false, type: "float" }]);
                });
            it("sniffs floats, decimals and integers as float",
                function () {
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
                    expect(datatypeSniffer.getDatatypes(data))
                        .toEqual([{ hasEmptyValues: false, allEmptyValues: false, type: "float" }]);
                });
            it("sniffs date values",
                function() {
                    var data = [
                        ["dates"],
                        ["23/12/2020"],
                        ["23/12/20"],
                        ["12/23/2020"],
                        ["23/12/2020"],
                        ["2020-12-23"],
                        ["-0163-05-13"]
                    ];
                    expect(datatypeSniffer.getDatatypes(data))
                        .toEqual([{ hasEmptyValues: false, allEmptyValues: false, type: "date" }]);
                });
            it("sniffs date/time values",
                function() {
                    var data = [
                        ["datetimes"],
                        ["2020-12-23T02:01:00Z"],
                        ["2020-12-23T02:01:00-11:00"],
                        ["2020-12-23T02:01:00+09:00"],
                        ["2020-12-23T02:01:00.1"],
                        ["2020-12-23T02:01:00.01"]
                    ];
                    expect(datatypeSniffer.getDatatypes(data))
                        .toEqual([{ hasEmptyValues: false, allEmptyValues: false, type: "datetime" }]);
                });
            it("sniffs mixed date and datetime as string",
                function() {
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
                    expect(datatypeSniffer.getDatatypes(data))
                        .toEqual([{ hasEmptyValues: false, allEmptyValues: false, type: "string" }]);
                });
        });
});