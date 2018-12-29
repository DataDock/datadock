
(function($) {

    metadataEditor = function(opts) {

        this.opts = opts;
        this.chunks = 0;
        this.stepped = 0;
        this.rows = 0;
        this.columnCount = 0;
        this.pauseChecked = false;
        this.printStepChecked = false;
        this.filename = "";
        this.csvFile = null;
        this.csvData = null;
        this.header = [];
        this.columnSet = [];
        this.schemaTitle = null;
        this.templateMetadata = null;

        var self = this;
        // event subscriptions

        $("#fileSelectTextBox").click(function(e) {
            $("input:file", $(e.target).parents()).click();
        });

        $("#fileSelectButton").click(function(e) {
            $("input:file", $(e.target).parents()).click();
        });

        // add types to dForm
        $.dform.subscribe("changeTab",
            function(options, type) {
                if (options !== "") {
                    this.click(function() {
                        hideAllTabContent();
                        $("#" + options).show();
                        $("#" + options + "Tab").addClass("active");
                        return false;
                    });
                }

            });

        $.dform.subscribe("updateDatasetId",
            function(options, type) {
                if (options !== "") {
                    this.keyup(function() {
                        var title = $("#datasetTitle").val();
                        var slug = slugify(title, "", "", "camelCase");
                        var datasetId = getPrefix() + "/id/dataset/" + slug;
                        $("#datasetId").val(datasetId);
                        return false;
                    });
                }

            });

        $("#fileSelect").on("change",
            function() {
                clearErrors();
                self.stepped = 0;
                self.chunks = 0;
                self.rows = 0;

                // todo check that the file input can only select CSV
                var files = $("#fileSelect")[0].files;
                var config = buildConfig();

                // pauseChecked = $("#step-pause").prop("checked");
                // printStepChecked = $("#print-steps").prop("checked");


                if (files.length > 0) {
                    var file = files[0];
                    if (file.size > 1024 * 1024 * 10) {
                        displaySingleError("File size is over the 4MB limit. Reduce file size before trying again.");
                        return false;
                    }

                    Papa.parse(file, config);
                } else {
                    displaySingleError("No file found. Please try again.");
                }
            });

        $("#submit-unparse").click(function() {
            var input = $("#input").val();
            var delim = $("#delimiter").val();
            var header = $("#header").prop("checked");

            var results = Papa.unparse(input,
                {
                    delimiter: delim,
                    header: header
                });

            console.log("Unparse complete!");
            console.log("--------------------------------------");
            console.log(results);
            console.log("--------------------------------------");
        });

        $("#insert-tab").click(function() {
            $("#delimiter").val("\t");
        });

        begin();

    }

    this.begin = function() {
        showLoading();
        if (this.opts.schemaId) {
            loadSchemaBeforeDisplay();
        } else {
            displayFileSelector();
        }
    }

    this.displayFileSelector = function() {
        if (this.schemaTitle) {
            $("#templateTitle").html(this.schemaTitle);
            $("#metadataEditorForm").addClass("info");
            $("#templateInfoMessage").show();
        }
        showStep1();
    }

    this.constructCsvwMetadata = function() {
        var csvw = {};
        csvw["@context"] = "http://www.w3.org/ns/csvw";

        var datasetId = $("#datasetId").val();

        csvw["url"] = datasetId;

        csvw["dc:title"] = $("#datasetTitle").val();

        csvw["dc:description"] = $("#datasetDescription").val();

        var keywords = $("#keywords").val();
        if (keywords) {
            if (keywords.indexOf(",") < 0) {
                csvw["dcat:keyword"] = [keywords];
            } else {
                var keywordsArray = keywords.split(",");
                csvw["dcat:keyword"] = keywordsArray;
            }
        }

        csvw["dc:license"] = $("#datasetLicense").val();

        var aboutUrl = $('#aboutUrlSuffix').val();
        csvw["aboutUrl"] = aboutUrl;

        csvw["tableSchema"] = constructCsvwtableSchema();

        console.log(csvw);
        return csvw;
    }

    this.constructCsvwtableSchema = function() {
        var tableSchema = {};

        var columns = [];

        console.log(columnSet);
        if (this.columnSet) {
            for (var i = 0; i < this.columnSet.length; i++) {
                var colName = this.columnSet[i];
                var colId = "#" + colName;
                var skip = $(colId + "_suppress").prop("checked");
                var col = constructCsvwColumn(colName, skip);
                columns.push(col);
            }
        }
        tableSchema["columns"] = columns;

        return tableSchema;
    }

    this.constructCsvwColumn = function(columnName, skip) {
        var colId = "#" + columnName;
        var datatype = $(colId + "_datatype").val();
        var column = {};
        column["name"] = columnName;
        if (skip) {
            column["suppressOutput"] = true;
        } else {

            var columnTitle = $(colId + "_title").val();
            column["titles"] = [columnTitle];

            if (datatype === "uri") {
                column["valueUrl"] = "{" + columnName + "}";
            } else {
                column["datatype"] = $(colId + "_datatype").val();
            }

            column["propertyUrl"] = $(colId + "_property_url").val();
        }
        return column;
    }

    this.sendData = function(e) {
        clearErrors();

        $("#step2").removeClass("active");
        $("#step3").addClass("active");

        var formData = new FormData();
        formData.append("ownerId", this.opts.ownerId); // global variable set on Import.cshtml
        formData.append("repoId", this.opts.repoId); // global variable set on Import.cshtml
        formData.append("file", this.csvFile, this.filename);
        formData.append("filename", this.filename);
        formData.append("metadata", JSON.stringify(constructCsvwMetadata()));
        formData.append("showOnHomePage", JSON.stringify($("#showOnHomepage").prop("checked")));
        formData.append("saveAsSchema", JSON.stringify($("#saveAsTemplate").prop("checked")));
        formData.append("addToExisting", JSON.stringify($("#addToExistingData").prop("checked")));

        var apiOptions = {
            url: "/api/data",
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function(r) {
                sendDataSuccess(r);
            },
            error: function(r) {
                sendDataFailure(r);
            }
        };

        $("#metadataEditor").hide();
        $("#loading").show();
        $.ajax(apiOptions);

        return false;
    }

    this.sendDataSuccess = function(response) {
        var jobsUrl = "/dashboard/jobs/" + this.opts.ownerId + "/" + this.opts.repoId;
        if (this.opts.baseUrl) {
            jobsUrl = this.opts.baseUrl + jobsUrl;
        }
        if (response) {
            if (response["statusCode"] === 200) {
                var jobIds = response["jobIds"];
                if (jobIds) {
                    jobsUrl = jobsUrl + "/" + jobIds;
                } else {
                    jobsUrl = jobsUrl + "/latest";
                }
            }
            window.location.href = jobsUrl;
        } else {
            $("#warning-messages ul li:last")
                .append(
                    "<li><span>The job has been successfully started but we cannot redirect you automatically, please check the job history page for more information on the publishing process.</span></li>");
            //todo show warnings
        }

    }

    this.sendDataFailure = function(response) {
        $("#metadataEditor").show();
        $("#loading").hide();

        if (response) {
            var responseMsg = response["responseText"];
            displaySingleError("Publish data API has reported an error: " + responseMsg);
        } else {
            displaySingleError("Publish data API has resulted in an unspecified error.");
        }
    }

//papaparse
    this.buildConfig = function() {
        var config = {
            header: false,
            preview: 0,
            delimiter: $("#delimiter").val(),
            newline: getLineEnding(),
            comments: $("#comments").val(),
            encoding: $("#encoding").val(),
            worker: false,
            step: undefined,
            complete: function(results, parser) {
                self.completeFn(results, parser);
            },
            error: errorFn,
            download: false,
            skipEmptyLines: true,
            //chunk: $('#chunk').prop('checked') ? chunkFn : undefined,
            chunk: undefined,
            beforeFirstChunk: undefined
        };
        return config;

        function getLineEnding() {
            if ($("#newline-n").is(":checked"))
                return "\n";
            else if ($("#newline-r").is(":checked"))
                return "\r";
            else if ($("#newline-rn").is(":checked"))
                return "\r\n";
            else
                return "";
        }


    }

    this.stepFn = function(results, parserHandle) {
        this.stepped++;
        this.rows += results.data.length;
        this.parser = parserHandle;

        if (this.pauseChecked) {
            //console.log(results, results.data[0]);
            parserHandle.pause();
            return;
        }

        if (this.printStepChecked) {
            //console.log(results, results.data[0]);
        }

    }

    this.chunkFn = function(results, streamer, file) {
        if (!results) return;

        this.chunks++;
        this.rows += results.data.length;
        this.parser = streamer;

        if (this.printStepChecked)
            console.log("Chunk data:", results.data.length, results);

        if (this.pauseChecked) {
            console.log("Pausing; " + results.data.length + " rows in chunk; file:", file);
            streamer.pause();
            return;
        }
    }

    this.errorFn = function (error, file) {
        // TODO: Surface parsing errors to the end user
        console.log("ERROR:", error, file);
    }

    this.completeFn = function(results, file) {
        if (!$("#stream").prop("checked") && !$("#chunk").prop("checked") && arguments[0] && arguments[0].data) {
            self.rows = results.data.length;
        }
        self.csvData = results.data;
        self.filename = file.name;
        self.csvFile = file;
        if (self.csvData) {
            self.header = self.csvData[0];
            self.columnCount = self.header.length;
        }
        loadEditor();
    }
//end papaparse

//jquery.dform
    this.loadEditor = function() {

        this.columnSet = [];

        var datasetInfoTabContent = constructBasicTabContent();
        var identifiersTabContent = constructIdentifiersTabContent();
        var columnDefinitionsTabContent = constructColumnDefinitionsTabContent();
        var advancedTabContent = constructAdvancedTabContent();
        var previewTabContent = constructPreviewTabContent();

        var submitButton = {
            "type": "div",
            "class": "ui center aligned container",
            "html": [
                {
                    "type": "div",
                    "class": "ui hidden divider",
                    "html": ""
                },
                {
                    "type": "div",
                    "class": "ui buttons",
                    "html": [
                        {
                            "type": "submit",
                            "id": "publish",
                            "class": "ui primary button large",
                            "publish": "sendData",
                            "value": "Publish Data"
                        }
                    ]
                }
            ]
        };

        var tabs = {
            "type": "div",
            "class": "four wide column",
            "html": {
                "type": "div",
                "class": "ui vertical pointing menu",
                "html": [
                    {
                        "type": "a",
                        "html": "Dataset Details",
                        "class": "item",
                        "id": "datasetInfoTab",
                        "changeTab": "datasetInfo"
                    },
                    {
                        "type": "a",
                        "html": "Identifiers",
                        "class": "item",
                        "id": "identifierTab",
                        "changeTab": "identifier"
                    },
                    {
                        "type": "a",
                        "html": "Column Definitions",
                        "class": "item",
                        "id": "columnDefinitionsTab",
                        "changeTab": "columnDefinitions"
                    },
                    {
                        "type": "a",
                        "html": "Advanced",
                        "class": "item",
                        "id": "advancedTab",
                        "changeTab": "advanced"
                    },
                    {
                        "type": "a",
                        "html": "Data Preview",
                        "class": "item",
                        "id": "previewTab",
                        "changeTab": "preview"
                    }
                ]
            }
        };
        var tabsContent = {
            "type": "div",
            "class": "twelve wide stretched column",
            "html": {
                "type": "div",
                "class": "tabcontent",
                "html": [
                    {
                        "type": "div",
                        "id": "datasetInfo",
                        "html": datasetInfoTabContent
                    },
                    {
                        "type": "div",
                        "id": "identifier",
                        "html": identifiersTabContent
                    },
                    {
                        "type": "div",
                        "id": "columnDefinitions",
                        "html": columnDefinitionsTabContent
                    },
                    {
                        "type": "div",
                        "id": "advanced",
                        "html": advancedTabContent
                    },
                    {
                        "type": "div",
                        "id": "preview",
                        "html": previewTabContent
                    }
                ]
            }
        };


        var mainForm = {
            "type": "div",
            "class": "ui stackable two column grid container",
            "html": [tabs, tabsContent]
        };

        var configCheckboxes = constructPublishOptionsCheckboxes();

        var formTemplate = {
            "class": "ui form",
            "method": "POST",
            "validate": {
                debug: true,
                ignore: ".skip-validation",
                onkeyup: false,
                onfocusout: false,
                onclick: false,
                showErrors: function(errorMap, errorList) {
                    console.log(errorMap);
                    console.log(errorList);
                    var numErrors = this.numberOfInvalids();
                    if (numErrors) {
                        var validationMessage = numErrors === 1
                            ? "1 field is missing or invalid, please correct this before submitting your data."
                            : numErrors +
                            " fields are missing or invalid, please correct this before submitting your data.";

                        $("#validation-messages").html(validationMessage);
                        var invalidFieldList = $("<ul />");
                        $.each(errorList,
                            function(i) {
                                var error = errorList[i];
                                $("<li/>")
                                    .addClass("invalid-field")
                                    .appendTo(invalidFieldList)
                                    .text(error.message);
                            });
                        $("#validation-messages").append(invalidFieldList);
                        $("#validation-messages").css("margin", "0.5em");
                        $("#validation-messages").show();
                        this.defaultShowErrors();
                    } else {
                        $("#validation-messages").hide();
                    }
                },
                errorPlacement: function(error, element) {
                    error.insertBefore(element);
                },
                highlight: function(element, errorClass, validClass) {
                    $(element).parents(".field").addClass(errorClass);
                },
                unhighlight: function(element, errorClass, validClass) {
                    $(element).parents(".field").removeClass(errorClass);
                },
                submitHandler: function(e) {
                    sendData(e);
                }
            }
        }

        formTemplate.html = [mainForm, configCheckboxes, submitButton];

        $("#metadataEditorForm").dform(formTemplate);

        // set selected license from template
        if (this.templateMetadata) {
            var licenseFromTemplate = schemaHelper.getLicenseUri(this.templateMetadata, "");
            if (licenseFromTemplate) {
                $("#datasetLicense").val(licenseFromTemplate);
            }
        }
        // set the column datatypes from the template
        setDatatypesFromTemplate();
        // set the aboutUrl from the template
        if (this.templateMetadata) {
            $("#aboutUrlSuffix").val(templateMetadata["aboutUrl"]);
        }

        showStep2();

        // show first tab
        hideAllTabContent();
        $("#datasetInfo").show();
        $("#datasetInfoTab").addClass("active");

        // inputosaurus
        $("#keywords").inputosaurus({
            inputDelimiters: [",", ";"],
            width: "100%",
            change: function(ev) {
                $("#keywords_reflect").val(ev.target.value);
            }
        });

        // Prevent form submission when the user presses enter
        $("#metadataEditorForm").on("keypress",
            ":input:not(textarea)",
            function(event) {
                if (event.keyCode === 13) {
                    event.preventDefault();
                }
            });

    }

    this.constructBasicTabContent = function() {
        var datasetVoidFields = [
            {
                "type": "div",
                "class": "field",
                "html": {
                    "name": "datasetTitle",
                    "id": "datasetTitle",
                    "caption": "Title",
                    "type": "text",
                    "updateDatasetId": "this",
                    "value": schemaHelper.getTitle(this.templateMetadata, this.filename),
                    "validate": {
                        "required": true,
                        "minlength": 2,
                        "messages": {
                            "required": "You must enter a title",
                            "minlength": "The title must be at least 2 characters long"
                        }
                    }
                }
            },
            {
                "type": "div",
                "class": "field",
                "html": {
                    "name": "datasetDescription",
                    "id": "datasetDescription",
                    "caption": "Description",
                    "type": "textarea",
                    "value": schemaHelper.getDescription(this.templateMetadata, "")
                }
            },
            {
                "type": "div",
                "class": "field",
                "html": {
                    "name": "datasetLicense",
                    "id": "datasetLicense",
                    "caption": "License",
                    "type": "select",
                    "options": {
                        "": "Please select a license",
                        "https://creativecommons.org/publicdomain/zero/1.0/": "Public Domain (CC-0)",
                        "https://creativecommons.org/licenses/by/4.0/": "Attribution (CC-BY)",
                        "https://creativecommons.org/licenses/by-sa/4.0/": "Attribution-ShareAlike (CC-BY-SA)",
                        "http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/":
                            "Open Government License (OGL)",
                        "https://opendatacommons.org/licenses/pddl/":
                            "Open Data Commons Public Domain Dedication and License (PDDL)",
                        "https://opendatacommons.org/licenses/by/": "Open Data Commons Attribution License (ODC-By)"
                    },
                    "validate": {
                        "required": true,
                        "messages": {
                            "required": "You must select a license"
                        }
                    }
                }
            },
            {
                "type": "div",
                "class": "field",
                "html": {
                    "name": "keywords",
                    "id": "keywords",
                    "caption": "Keywords (separate using commas)",
                    "type": "text",
                    "value": schemaHelper.getTags(this.templateMetadata)
                }
            }
        ];
        return datasetVoidFields;
    }

    this.constructIdentifiersTabContent = function() {
        var prefix = getPrefix();
        var datasetId = slugify(this.filename, "", "", "camelCase");
        var idFromFilename = prefix + "/id/dataset/" + datasetId;
        var defaultValue = schemaHelper.getDatasetId(this.templateMetadata, idFromFilename);

        var dsIdTable = {
            "type": "table",
            "class": "ui celled table",
            "html": [
                {
                    "type": "thead",
                    "html": {
                        "type": "tr",
                        "html": {
                            "type": "th",
                            "html": "Dataset Identifier (readonly)"
                        }
                    }
                },
                {
                    "type": "tbody",
                    "html": [
                        {
                            "type": "tr",
                            "html": {
                                "type": "td",
                                "html": {
                                    "type": "div",
                                    "class": "field",
                                    "html": {
                                        "type": "text",
                                        "readonly": true,
                                        "id": "datasetId",
                                        "name": "datasetId",
                                        "value": defaultValue,
                                        "disabled": true
                                    }
                                }
                            }
                        },
                        {
                            "type": "tr",
                            "html": {
                                "type": "td",
                                "html":
                                    "The identifier for the dataset is constructed from the chosen title, the GitHub repository, and the GitHub user or organization you're uploading the data to."
                            }
                        }
                    ]
                }
            ]
        };


        var identifierTableElements = [];
        identifierTableElements.push(
            {
                "type": "thead",
                "html":
                [
                    {
                        "type": "tr",
                        "html": [
                            {
                                "type": "th",
                                "html": "Construct individual record identifiers from which column's values?"
                            }
                        ]
                    }
                ]
            }
        );

        var rowIdentifier = getIdentifierPrefix() + "/" + datasetId + "/row_{_row}";
        var identifierOptions = {};
        identifierOptions[rowIdentifier] = "Row Number";

        for (var colIdx = 0; colIdx < this.columnCount; colIdx++) {
            var colTitle = this.header[colIdx];
            var colName = slugify(colTitle, "_", "_", "lowercase");
            var colIdentifier = getIdentifierPrefix() + "/" + colName + "/{" + colName + "}";
            identifierOptions[colIdentifier] = colTitle;
        }
        identifierTableElements.push(
            {
                "type": "tbody",
                "html":
                [
                    {
                        "type": "tr",
                        "html": [
                            {
                                "type": "td",
                                "html": [
                                    {
                                        name: "aboutUrlSuffix",
                                        id: "aboutUrlSuffix",
                                        type: "select",
                                        placeholder: "",
                                        options: identifierOptions
                                    }
                                ]
                            }
                        ]
                    },
                    {
                        "type": "tr",
                        "html": [
                            {
                                "type": "td",
                                "html":
                                    "You must be sure that there are no empty values in the data to use it as the basis for a record's identifier. We suggest using an ID field if you have one. If in doubt, use the default (the row number). <br />Identifiers can be crucial when it comes to linking between records of different datasets; <a href=\"https://github.com/DataDock/datadock/wiki/selecting-an-identifier\" title=\"DataDock Documentation: Identifiers\" target=\"_blank\">You can read more about identifiers here (opens in new window)</a>."
                            }
                        ]
                    }
                ]
            }
        );
        var identifierTable = { "type": "table", "html": identifierTableElements, "class": "ui celled table" };
        var identifierSection = [];
        identifierSection.push(
            {
                "type": "div",
                "html":
                    [dsIdTable, identifierTable]
            }
        );
        return identifierSection;
    }

    this.constructColumnDefinitionsTabContent = function() {
        var columnDefinitionsTableElements = [];

        columnDefinitionsTableElements.push(
            {
                "type": "thead",
                "html":
                [
                    {
                        "type": "tr",
                        "html": [
                            {
                                "type": "th",
                                "html": "Title"
                            },
                            {
                                "type": "th",
                                "html": "DataType"
                            },
                            {
                                "type": "th",
                                "html": "Suppress In Output"
                            }
                        ]
                    }
                ]
            }
        );
        for (var colIdx = 0; colIdx < this.columnCount; colIdx++) {

            var trElements = [];
            var colTitle = this.header[colIdx];
            var colName = slugify(colTitle, "_", "_", "lowercase");

            this.columnSet.push(colName);

            var colTemplate = schemaHelper.getColumnTemplate(this.templateMetadata, colName);
            var defaultTitleValue = schemaHelper.getColumnTitle(colTemplate, colTitle);
            var titleField = {
                name: colName + "_title",
                id: colName + "_title",
                type: "text",
                placeholder: "",
                value: defaultTitleValue,
                "validate": {
                    "required": true,
                    "messages": {
                        "required": "Column '" + colName + "' is missing a title"
                    }
                }
            };
            var tdTitle = { "type": "td", "html": titleField };
            trElements.push(tdTitle);

            var datatypeField = {
                name: colName + "_datatype",
                id: colName + "_datatype",
                type: "select",
                placeholder: "",
                options: {
                    "string": "Text",
                    "uri": "URI",
                    "integer": "Whole Number",
                    "decimal": "Decimal Number",
                    "date": "Date",
                    "datetime": "Data & Time",
                    "boolean": "True/False"
                }
            };
            var tdDatatype = { "type": "td", "html": datatypeField };
            trElements.push(tdDatatype);

            var suppressField = {
                name: colName + "_suppress",
                id: colName + "_suppress",
                type: "checkbox",
                "class": "center aligned"
            };
            var suppressedInTemplate = schemaHelper.getColumnSuppressed(colTemplate);
            if (suppressedInTemplate) {
                suppressField["checked"] = "checked";
            }
            var tdSuppress = { "type": "td", "html": suppressField };
            trElements.push(tdSuppress);

            var tr = { "type": "tr", "html": trElements };
            columnDefinitionsTableElements.push(tr);
        }
        var columnDefinitionsTable =
            { "type": "table", "html": columnDefinitionsTableElements, "class": "ui celled table" };
        return columnDefinitionsTable;
    }

    this.constructAdvancedTabContent = function() {
        var predicateTableElements = [];
        predicateTableElements.push(
            {
                "type": "thead",
                "html":
                [
                    {
                        "type": "tr",
                        "html": [
                            {
                                "type": "th",
                                "html": "Column"
                            },
                            {
                                "type": "th",
                                "html": "Property (URL)"
                            }
                        ]
                    }
                ]
            }
        );
        for (var colIdx = 0; colIdx < columnCount; colIdx++) {

            var trElements = [];
            var colTitle = this.header[colIdx];
            var colName = slugify(colTitle, "_", "_", "lowercase");

            var titleDiv = {
                type: "div",
                html: colTitle
            };
            var tdTitle = { "type": "td", "html": titleDiv };
            trElements.push(tdTitle);

            var predicate = getPrefix() + "/id/definition/" + colName;
            var colTemplate = schemaHelper.getColumnTemplate(this.templateMetadata, colName);
            var defaultValue = schemaHelper.getColumnPropertyUrl(colTemplate, predicate);
            var predicateField = {
                name: colName + "_property_url",
                id: colName + "_property_url",
                type: "text",
                placeholder: "",
                "value": defaultValue,
                "class": "pred-field",
                "validate": {
                    "required": true,
                    "pattern": /^https?:\/\/\S+[^#\/]$/i,
                    "messages": {
                        "required": "Column '" + colName + "' is missing a property URL.",
                        "pattern": "Column '" +
                            colName +
                            "' must have a property URL that is a URL that does not end with a hash or slash."
                    }
                }
            };
            var predDiv = { "type": "div", "class": "field", "html": predicateField };
            var tdPredicate = { "type": "td", "html": predDiv };
            trElements.push(tdPredicate);

            var tr = { "type": "tr", "html": trElements };
            predicateTableElements.push(tr);
        }
        var predicateTable = { "type": "table", "html": predicateTableElements, "class": "ui celled table" };
        var advancedSection = [];
        advancedSection.push(
            {
                "type": "div",
                "html":
                    [predicateTable]
            }
        );
        return advancedSection;

    }

    this.constructPreviewTabContent = function() {

        var ths = [];
        for (var i = 0; i < this.header.length; i++) {
            var th = {
                "type": "th",
                "html": this.header[i]
            };
            ths.push(th);
        }
        var thead = {
            "type": "thead",
            "html": {
                "type": "tr",
                html: ths
            }
        };
        var rows = [];
        var displayRowCount = 50;
        var originalRowCount = this.csvData.length - 1; // row 0 is header
        var originPlural = "s";
        if (originalRowCount === 1) {
            originPlural = "";
        }

        if (originalRowCount < displayRowCount) {
            displayRowCount = originalRowCount;
        }
        var displayPlural = "s";
        if (displayRowCount === 1) {
            displayPlural = "";
        }

        for (var i = 1; i < displayRowCount + 1; i++) {
            var rowData = this.csvData[i];
            var tds = [];
            for (var j = 0; j < rowData.length; j++) {
                var td = {
                    "type": "td",
                    "class": "top aligned preview",
                    "html": rowData[j]
                };
                tds.push(td);
            }
            var row = {
                "type": "tr",
                "html": tds
            };
            rows.push(row);
        }
        var tbody = {
            "type": "tbody",
            "html": rows
        };

        var previewTable = {
            "type": "table",
            "class": "ui celled striped compact table",
            "html": [thead, tbody]
        };

        var infoMessage = {
            "type": "div",
            "class": "ui info message",
            "html":
                "<p>The file contains " +
                    originalRowCount +
                    " row" +
                    originPlural +
                    " of data, previewing " +
                    displayRowCount +
                    " row" +
                    displayPlural +
                    " of data below: </p>"
        };

        var container = {
            "type": "div",
            "class": "ui container data-preview",
            "style": "overflow-x: scroll;",
            "html": [infoMessage, previewTable]
        };
        return container;
    }

    this.constructPublishOptionsCheckboxes = function() {

        var showOnHomepage = {
            "type": "div",
            "html": {
                "type": "div",
                "class": "ui checkbox",
                "html": {
                    "type": "checkbox",
                    "name": "showOnHomepage",
                    "id": "showOnHomepage",
                    "caption": "Include my published dataset on DataDock homepage and search",
                    "value": true
                }
            }
        };
        var addToData = {
            "type": "div",
            "html": {
                "type": "div",
                "class": "ui checkbox",
                "html": {
                    "type": "checkbox",
                    "name": "addToExistingData",
                    "id": "addToExistingData",
                    "caption": "Add to existing data if dataset already exists (default is to overwrite existing data)",
                    "value": false
                }
            }
        };
        var saveAsTemplate = {
            "type": "div",
            "html": {
                "type": "div",
                "class": "ui checkbox",
                "html": {
                    "type": "checkbox",
                    "name": "saveAsTemplate",
                    "id": "saveAsTemplate",
                    "caption": "Save this information as a template for future imports",
                    "value": false
                }
            }
        };
        var divider = {
            "type": "div",
            "class": "ui divider",
            "html": ""
        };
        var hiddenDivider = {
            "type": "div",
            "class": "ui hidden divider",
            "html": ""
        };
        var configCheckboxes = {
            "type": "div",
            "class": "ui center aligned container",
            "html": [divider, addToData, hiddenDivider, showOnHomepage, hiddenDivider, saveAsTemplate]
        };
        return configCheckboxes;
    }
//end jquery.dform

//helper functions
    this.getPrefix = function() {
        return this.opts.publishUrl + "/" + this.opts.ownerId + "/" + this.opts.repoId;
    }

    this.getIdentifierPrefix = function() {
        return getPrefix() + "/id/resource";
    }

    this.slugify = function(original, whitespaceReplacement, specCharReplacement, casing) {
        switch (casing) {
        case "lowercase":
            var lowercase = original.replace(/\s+/g, whitespaceReplacement).replace(/[^A-Z0-9]+/ig, specCharReplacement)
                .replace("__", "_").toLowerCase();
            return lowercase;

        case "camelCase":
            var camelCase = camelize(original);
            return camelCase;

        default:
            var slug = original.replace(/\s+/g, whitespaceReplacement).replace(/[^A-Z0-9]+/ig, specCharReplacement)
                .replace("__", "_");
            return slug;
        }
    }

    this.camelize = function(str) {
        var camelised = str.split(/[\s_\-]/).map(function(word, index) {
            if (index === 0) {
                return word.toLowerCase();
            }
            return word.charAt(0).toUpperCase() + word.slice(1).toLowerCase();
        }).join("");
        var slug = camelised.replace(/[^A-Z0-9]+/ig, "");
        return slug;
    }

    this.isArray = function(value) {
        return value && typeof value === 'object' && value.constructor === Array;
    }

    this.sniffDatatype = function() {

    }
//end helper functions

//ui and window location functions
    this.hideAllTabContent = function() {
        $("#datasetInfo").hide();
        $("#datasetInfoTab").removeClass("active");
        $("#columnDefinitions").hide();
        $("#columnDefinitionsTab").removeClass("active");
        $("#identifier").hide();
        $("#identifierTab").removeClass("active");
        $("#advanced").hide();
        $("#advancedTab").removeClass("active");
        $("#preview").hide();
        $("#previewTab").removeClass("active");
    }

    this.showStep1 = function() {
        $("#fileSelector").show();
        $("#metadataEditor").hide();
        $("#loading").hide();
    }

    this.showStep2 = function() {
        $("#fileSelector").hide();
        $("#metadataEditor").show();
        $("#step1").removeClass("active");
        $("#step2").addClass("active");
        $("#loading").hide();
    }

    this.showLoading = function() {
        $("#fileSelector").hide();
        $("#metadataEditor").hide();
        $("#loading").show();
    }

    this.displaySingleError = function(error) {
        //console.error(error);
        $("#error-messages").append("<div><i class=\"warning sign icon\"></i><span>" + error + "</span></div>");
        $("#error-messages").show();
    }

    this.displayErrors = function(errors) {
        if (errors) {
            $("#error-messages").append("<div><i class=\"warning sign icon\"></i></div>");
            var list = $("<ul/>");
            $.each(errors,
                function(i) {
                    $('<li />', { html: errors[i] }).appendTo(list);
                });
            list.appendTo("#error-messages");
        }
        $("#error-messages").show();
    }

    this.clearErrors = function() {
        $("#error-messages").html("");
        $("#error-messages").hide();
    }

    this.setDatatypesFromTemplate = function() {
        if (this.columnSet && this.templateMetadata) {
            for (var i = 0; i < this.columnSet.length; i++) {
                var colName = this.columnSet[i];
                var colTemplate = schemaHelper.getColumnTemplate(this.templateMetadata, colName);
                var colDatatype = schemaHelper.getColumnDatatype(colTemplate, "");
                if (colDatatype) {
                    var selector = $("#" + colName + "_datatype");
                    if (selector) {
                        selector.val(colDatatype);
                    }
                }
            }
        }
    }
//end ui functions

//schema/template functions 
    this.loadSchemaBeforeDisplay = function () {
        var self = this;
        if (this.opts.schemaId) {
            var options = {
                url: "/api/schemas",
                type: "get",
                data: {
                    "ownerId": this.opts.ownerId,
                    "schemaId": this.opts.schemaId
                },
                success: function(response) {
                    console.log("Template returned from DataDock schema API");
                    console.log(response);
                    if (response["schema"] && response["schema"]["metadata"]) {
                        self.schemaTitle = response["schema"]["dc:title"] || "";
                        self.templateMetadata = response["schema"]["metadata"];
                    } else {
                        console.error("Could not find a template in the response");
                    }
                    // now build form
                    displayFileSelector();
                },
                error: function(response) {
                    console.error("Unable to retrieve template from DataDock schema API");
                    console.error(response);
                    // build form without schema 
                    // todo show error message about missing schema
                    displayFileSelector();
                }
            };
            $.ajax(options);
        } else {
            displayFileSelector();
        }
    }

//end schema/template functions
})(jQuery);