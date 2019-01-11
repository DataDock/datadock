describe("metadata editor", function () {
    describe("on start up", function() {
        var editor;
        beforeEach(function () {
            $('body').append('<div id="metadataEditor"></div>');
            editor = $('#metadataEditor').metadataEditor({
                "baseUrl": "http://example.org/",
                "publishUrl": "http://datadock.io/",
                "ownerId": "owner",
                "repoId": "repo",
                "schemaId": null,
                "apiUrl": "http://example.org/api/data",
                "csvData": [
                    ["Name", "Age", "Email"],
                    ["Alice", "35", "alice@example.org"],
                    ["Bob", "21", "bob@example.org"]
                ],
                header: ["Name", "Age", "Email"],
                filename: "people.csv",
                schemaTitle: null,
                templateMetadata: null
            });
        });
        afterEach(function() {
            $('#metadataEditor').remove();
        });
        it("shows the dataset info tab", function() { expect($("#datasetInfo").is(":visible")).toBe(true) });
        it("hides the columnDefinitions tab", function() { expect($("#columnDefinitions").is(":hidden")).toBe(true) });
    });
});