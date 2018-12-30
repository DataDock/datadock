describe("metadata editor", function () {
    it("starts up",
        function() {
            metadataEditor({
                "baseUrl": "http://example.org/",
                "publishUrl": "http://datadock.io/",
                "ownerId": "owner",
                "repoId": "repo",
                "schemaId": null,
                "apiUrl": "http://example.org/api/data"
            });
            expect(window.opts.baseUrl).toBe("http://example.org/");
            expect(window.opts.publishUrl).toBe("http://datadock.io/");
            expect(window.opts.ownerId).toBe("owner");
            expect(window.opts.repoId).toBe("repo");
            expect(window.opts.schemaId).toBe(null);
            expect(window.opts.apiUrl).toBe("http://example.org/api/data");
        });
});