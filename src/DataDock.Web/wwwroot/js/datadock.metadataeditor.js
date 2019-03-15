
(function ($) {

    $.widget("dd.controller",
        {
            options: {
                baseUrl: null,
                publishUrl: null,
                ownerId: null,
                repoId: null,
                schemaId: null,
                apiUrl: null
            },

            _create: function() {
                var self = this;
                self.clearErrors();
                self.showStep1();
                self.loadSchema(function (schemaInfo) {
                    if (schemaInfo.success && self.options.schemaId) {
                        $("#templateTitle").html(schemaInfo.schemaTitle);
                        $("#metadataEditorForm").addClass("info");
                        $("#templateInfoMessage").show();
                    }
                    var loader = $("#fileSelector").loader({
                        schemaId: self.options.schemaId,
                        schemaTitle: schemaInfo.schemaTitle,
                        templateMetadata: schemaInfo.templateMetadata
                    });

                    loader
                        .bind("loadererror",
                            function(ev, data) {
                                self.displayErrors([data.msg]);
                            })
                        .bind("loadercomplete",
                            function(ev, data) {
                                console.log(data);
                                self.loaderData = data;
                                if ($("#metadataEditor").is(":data('ddMetadataEditor')")) {
                                    // Reset the editor by destroying the widget and removing the dform-generated form
                                    $("#metadataEditor").metadataEditor("destroy");
                                    $('#metadataEditor form').remove();
                                    $('#metadataEditor').unbind("metadataeditorsubmit");
                                }
                                $("#metadataEditor").metadataEditor({
                                    baseUrl: self.options.baseUrl,
                                    publishUrl: self.options.publishUrl,
                                    ownerId: self.options.ownerId,
                                    repoId: self.options.repoId,
                                    schemaId: self.options.schemaId,
                                    apiUrl: self.options.apiUrl,
                                    csvData: data.csvData,
                                    header: data.csvData[0],
                                    filename: data.filename,
                                    schemaTitle: schemaInfo.schemaTitle,
                                    templateMetadata: schemaInfo.templateMetadata
                                }).bind("metadataeditorsubmit",
                                    function(ev, data) {
                                        console.log(data);
                                        self._sendData(self.loaderData, data);
                                    });
                                self.showStep2();
                            });
                });
            },

            loadSchema: function (onLoad) {
                var self = this;
                if (this.options.schemaId) {
                    var options = {
                        url: "/api/schemas",
                        type: "get",
                        data: {
                            "ownerId": this.options.ownerId,
                            "schemaId": this.options.schemaId
                        },
                        success: function(response) {
                            console.log("Template returned from DataDock schema API");
                            console.log(response);
                            if (response["schema"] && response["schema"]["metadata"]) {
                                schemaHelper.makeAbsolute(response.schema.metadata,
                                    self.options.publishUrl + "/" + self.options.ownerId + "/" + self.options.repoId + "/");
                                onLoad({
                                    success: true,
                                    schemaTitle: response["schema"]["dc:title"] || "",
                                    templateMetadata: response["schema"]["metadata"],
                                    errorMsg: null
                                });
                            } else {
                                onLoad({
                                    success: false,
                                    errorMsg: "Could not find a valid template in the response."
                                });
                            }
                        },
                        error: function(response) {
                            console.error("Unable to retrieve template from DataDock schema API");
                            console.error(response);
                            // build form without schema 
                            // todo show error message about missing schema
                            onLoad({
                                success: false,
                                errorMsg: "Unable to retrieve template from DataDock schema API."
                            });
                        }
                    };
                    $.ajax(options);
                } else {
                    onLoad({ success: true, schemaTitle: null, templateMetadata: null });
                }
            },

            showStep1: function() {
                $("#fileSelector").show();
                $("#metadataEditor").hide();
                $("#loading").hide();
            },

            showStep2: function() {
                $("#fileSelector").hide();
                $("#metadataEditor").show();
                $("#step1").removeClass("active");
                $("#step2").addClass("active");
                $("#loading").hide();
            },

            showLoading: function() {
                $("#fileSelector").hide();
                $("#metadataEditor").hide();
                $("#loading").show();
            },

            displaySingleError: function(error) {
                $("#error-messages").append("<div><i class=\"warning sign icon\"></i><span>" + error + "</span></div>");
                $("#error-messages").show();
            },

            displayErrors: function(errors) {
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
            },

            clearErrors: function() {
                $("#error-messages").html("");
                $("#error-messages").hide();
            },

            _sendData: function(loaderData, editorData) {
                var self = this;
                self.clearErrors();

                $("#step2").removeClass("active");
                $("#step3").addClass("active");

                var formData = new FormData();
                formData.append("ownerId", this.options.ownerId); // global variable set on Import.cshtml
                formData.append("repoId", this.options.repoId); // global variable set on Import.cshtml
                formData.append("file", loaderData.csvFile, loaderData.filename);
                formData.append("filename", loaderData.filename);
                formData.append("metadata", JSON.stringify(editorData.metadata));
                formData.append("showOnHomePage", JSON.stringify(editorData.showOnHomePage));
                formData.append("saveAsSchema", JSON.stringify(editorData.saveAsSchema));
                formData.append("addToExisting", JSON.stringify(editorData.addToExisting));
                formData.append("overwriteExisting", JSON.stringify(!editorData.addToExisting));

                var apiOptions = {
                    url: "/api/data",
                    type: "POST",
                    data: formData,
                    processData: false,
                    contentType: false,
                    success: function(r) {
                        self.sendDataSuccess(r);
                    },
                    error: function(r) {
                        self.sendDataFailure(r);
                    }
                };

                $("#metadataEditor").hide();
                $("#loading").show();
                $.ajax(apiOptions);

                return false;
            },

            sendDataSuccess: function(response) {
                var jobsUrl = "/dashboard/jobs/" + this.options.ownerId + "/" + this.options.repoId;
                if (this.options.baseUrl) {
                    jobsUrl = this.options.baseUrl + jobsUrl;
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
            },

            sendDataFailure: function(response) {
                $("#metadataEditor").show();
                $("#loading").hide();

                if (response) {
                    var responseMsg = response["responseText"];
                    this.displaySingleError("Publish data API has reported an error: " + responseMsg);
                } else {
                    this.displaySingleError("Publish data API has resulted in an unspecified error.");
                }
            }
        });

    $.widget("dd.loader",
        {
            options: {
                schemaId: null
            },

            _create: function() {
                var self = this;
               
                this.element.find("#fileSelectTextBox,#fileSelectButton").click(function(e) {
                    $("input:file", $(e.target).parents()).click();
                });

                $("#fileSelect").on("change",
                    function() {
                        // clearErrors();
                        // todo check that the file input can only select CSV
                        var files = this.files;
                        var config = {
                            header: false,
                            preview: 0,
                            delimiter: $("#delimiter").val(),
                            newline: self._getLineEnding(),
                            comments: $("#comments").val(),
                            encoding: $("#encoding").val(),
                            worker: false,
                            step: undefined,
                            complete: function (results, file) {
                                self._trigger("Complete",
                                    null,
                                    {
                                        "csvData": results.data,
                                        "filename": file.name,
                                        "csvFile": file
                                    });
                            },
                            error: function (error, file) {
                                self._trigger("error", null, { "msg": error, "filename": file.name, "csvFile": file });
                            },
                            download: false,
                            skipEmptyLines: true,
                            chunk: undefined,
                            beforeFirstChunk: undefined
                        };

                        if (files.length > 0) {
                            var file = files[0];
                            if (file.size > 1024 * 1024 * 10) {
                                self._trigger("Error",
                                    self.element,
                                    {
                                        "msg": "File size is over the 4MB limit. Reduce file size before trying again."
                                    });
                                return false;
                            }
                            Papa.parse(file, config);
                        } else {
                            self._trigger("Error", self.element, { "msg": "No file found. Please try again." });
                            return false;
                        }
                        return true;
                    });
            },

            _getLineEnding: function() {
                if ($("#newline-n").is(":checked"))
                    return "\n";
                else if ($("#newline-r").is(":checked"))
                    return "\r";
                else if ($("#newline-rn").is(":checked"))
                    return "\r\n";
                else
                    return "";
            }
        });

    $.widget("dd.metadataEditor",
        {
            options: {
                baseUrl: null,
                publishUrl: null,
                ownerId: null,
                repoId: null,
                schemaId: null,
                apiUrl: null,
                filename: null,
                csvData: null,
                templateMetadata: null,
                header: null
            },

            _create: function() {
                var self = this;
                this.columnSet = [];

                $("#step1").click(function(e) {
                    $("#fileSelector").show();
                    $("#metadataEditor").hide();
                    $("#loading").hide();
                });

                $.dform.subscribe("changeTab",
                    function(options, type) {
                        if (options !== "") {
                            this.click(function() {
                                self._hideAllTabContent();
                                $("#" + options).show();
                                $("#" + options + "Tab").addClass("active");
                                return false;
                            });
                        }

                    });

                $.dform.subscribe("updateDatasetId",
                    function(options, type) {
                        if (options !== "") {
                            this.keyup(function () {
                                self._updateDatasetIdFromTitle();
                                return false;
                            });
                        }

                    });

                $.dform.subscribe("updateDatatype",
                    function(colName, type) {
                        if (colName !== "") {
                            this.change(function () {
                                self._updateDatatype(colName, this.value);
                            });
                        }
                    });

                this._loadEditor();
                this._hideAllTabContent();
                this._updateDatasetIdFromTitle();
                $("#datasetInfo").show();
                $("#datasetInfoTab").addClass("active");
            },


            _updateDatasetIdFromTitle: function() {
                var title = $("#datasetTitle").val();
                var slug = this._slugify(title, "", "", "camelCase");
                var datasetId = this._getPrefix() + "/id/dataset/" + slug;
                $("#datasetId").val(datasetId);
            },

            _updateDatatype: function (colName, datatype) {
                $("#" + colName + "_advanced").children(".datatype-field").hide();
                $("#" + colName + "_advanced").children(".datatype-field.datatype-" + datatype).show();
                var options = { "default": "Default" };
                $(".datatype-selector").each(function(ix, selector) {
                    if ($(selector).val() === "uri" || $(selector).val() === "uriTemplate") {
                        var colName = $(selector).data("colname");
                        options[colName] = colName;
                    }
                });
                $(".parent-selector").each(function(ix, s) {
                    var selector = $(s);
                    var oldValue = selector.val();
                    $("option", selector).remove();
                    $.each(options, function(optVal, optLabel) {
                        selector.append("<option value='" + optVal + "'>" + optLabel + "</option>");
                    });
                    if (options.hasOwnProperty(oldValue)) {
                        selector.val(oldValue);
                    } else {
                        selector.val('default');
                    }
                });
                if (Object.keys(options).length > 1) {
                    $(".parent-field").show();
                } else {
                    $(".parent-field").hide();
                }
            },

            _hideAllTabContent: function() {
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
            },

            //jquery.dform
            _loadEditor: function() {

                var self = this;

                this.columnSet = [];

                var datasetInfoTabContent = this._constructBasicTabContent();
                var identifiersTabContent = this._constructIdentifiersTabContent();
                var columnDefinitionsTabContent = this._constructColumnDefinitionsTabContent();
                var advancedTabContent = this._constructAdvancedTabContent();
                var previewTabContent = this._constructPreviewTabContent();
                var configCheckboxes = this._constructPublishOptionsCheckboxes();

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
                            var data = {
                                "metadata": self._constructCsvwMetadata(),
                                "showOnHomePage": $("#showOnHomepage").prop("checked"),
                                "saveAsSchema": $("#saveAsTemplate").prop("checked"),
                                "addToExisting": $("#addToExistingData").prop("checked")
                            }
                            self._trigger("submit", null, data);
                        }
                    }
                }

                formTemplate.html = [mainForm, configCheckboxes, submitButton];

                this.element.dform(formTemplate);

                // set selected license from template
                if (this.options.templateMetadata) {
                    var licenseFromTemplate = schemaHelper.getLicenseUri(this.options.templateMetadata, "");
                    if (licenseFromTemplate) {
                        $("#datasetLicense").val(licenseFromTemplate);
                    }
                    // set the column datatypes from the template
                    this._setDatatypesFromTemplate();
                    // set the aboutUrl from the template
                    $("#aboutUrlSuffix").val(this.options.templateMetadata["aboutUrl"]);
                } else {
                    // set the column datatypes using the datatype sniffer
                    this._setDatatypesFromSniffer();
                }

                $('')

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


            },

            _constructBasicTabContent: function() {
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
                            "value": schemaHelper.getTitle(this.options.templateMetadata, this.options.filename),
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
                            "value": schemaHelper.getDescription(this.options.templateMetadata, "")
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
                                "https://opendatacommons.org/licenses/by/":
                                    "Open Data Commons Attribution License (ODC-By)"
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
                            "value": schemaHelper.getTags(this.options.templateMetadata)
                        }
                    }
                ];
                return datasetVoidFields;
            },

            _constructIdentifiersTabContent: function() {
                var prefix = this._getPrefix();
                var datasetId = this._slugify(this.options.filename, "", "", "camelCase");
                // KA: If we do this, then the owner and repo from the template gets used in the new dataset id which is probably not what the user wants
                // var idFromFilename = prefix + "/id/dataset/" + datasetId;
                // var defaultValue = schemaHelper.getDatasetId(this.options.templateMetadata, idFromFilename);
                var defaultValue = prefix + "/id/dataset/" + datasetId;

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

                var rowIdentifier = this._getIdentifierPrefix() + "/" + datasetId + "/row_{_row}";
                var identifierOptions = {};
                var columnCount = this.options.header.length;
                var aboutUrl = schemaHelper.getAboutUrl(this.options.templateMetadata);
                identifierOptions[rowIdentifier] = "Row Number";

                for (var colIdx = 0; colIdx < columnCount; colIdx++) {
                    var colTitle = this.options.header[colIdx];
                    var colName = this._slugify(colTitle, "_", "_", "lowercase");
                    var colIdentifier = this._getIdentifierPrefix() + "/" + colName + "/{" + colName + "}";
                    identifierOptions[colIdentifier] = colTitle;
                }

                if (aboutUrl && !identifierOptions.hasOwnProperty(aboutUrl)) {
                    identifierOptions[aboutUrl] = "Template pattern: " + aboutUrl;
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
            },

            _constructColumnDefinitionsTabContent: function() {
                var columnDefinitionsTableElements = [];

                columnDefinitionsTableElements.push(
                    {
                        "type": "thead",
                        "html":
                        [
                            {
                                "type": "tr",
                                "html": [
                                    { "type": "th", "html": "Column", "class": "collapsing" },
                                    { "type": "th", "html": "Title" },
                                    { "type": "th", "html": "DataType" },
                                    { "type": "th", "html": "Suppress In Output", "class": "collapsing" }
                                ]
                            }
                        ]
                    }
                );

                var columnCount = this.options.header.length;
                for (var colIdx = 0; colIdx < columnCount; colIdx++) {

                    var trElements = [];
                    var colTitle = this.options.header[colIdx];
                    var colName = this._slugify(colTitle, "_", "_", "lowercase");

                    this.columnSet.push(colName);

                    trElements.push({ "type": "td", "html": colName, "class": "collapsing" });
                    var colTemplate = schemaHelper.getColumnTemplate(this.options.templateMetadata, colName);
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
                        class: "datatype-selector",
                        "data-colName": colName,
                        placeholder: "",
                        options: {
                            "string": "Text",
                            "uri": "URI",
                            "integer": "Whole Number",
                            "decimal": "Decimal Number",
                            "date": "Date",
                            "datetime": "Date & Time",
                            "boolean": "True/False",
                            "uriTemplate": "URI Template"
                        },
                        updateDatatype: colName
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
            },

            _constructAdvancedTabContent: function() {
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
                                        "class": "collapsing",
                                        "html": "Column"
                                    },
                                    {
                                        "type": "th",
                                        "html": "Advanced Configuration"
                                    }
                                ]
                            }
                        ]
                    }
                );
                var columnCount = this.options.header.length;
                for (var colIdx = 0; colIdx < columnCount; colIdx++) {

                    var trElements = [];
                    var colTitle = this.options.header[colIdx];
                    var colName = this._slugify(colTitle, "_", "_", "lowercase");

                    var titleDiv = {
                        type: "div",
                        html: colTitle
                    };
                    var tdTitle = {
                        "type": "td",
                        "class": "collapsing",
                        "html": titleDiv
                    };
                    trElements.push(tdTitle);

                    var predicate = this._getPrefix() + "/id/definition/" + colName;
                    var colTemplate = schemaHelper.getColumnTemplate(this.options.templateMetadata, colName);
                    var defaultValue = schemaHelper.getColumnPropertyUrl(colTemplate, predicate);
                    var predicateField = {
                        type: "div",
                        class: "field",
                        html: [
                            {
                                type: "label",
                                for: colName + "_property_url",
                                html: "Property URL"
                            },
                            {
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
                            }
                        ]
                    };
                    var uriTemplateField = {
                        type: "div",
                        class: "field datatype-field datatype-uriTemplate",
                        hidden: true,
                        html: [
                            {
                                type: "label",
                                html: "URI Template",
                                for: colName + "_uriTemplate"
                            },
                            {
                                name: colName + "_uriTemplate",
                                id: colName + "_uriTemplate",
                                type: "text",
                                value: this._getIdentifierPrefix() + "/" + colName + "/{" + colName + "}",
                            }
                        ]
                    };
                    var parentField = {
                        type: "div",
                        class: "field parent-field",
                        hidden: true,
                        html: [
                            {
                                type: "label",
                                html: "Parent Node",
                                for: colName + "_parent"
                            },
                            {
                                name: colName + "_parent",
                                id: colName + "_parent",
                                class: "parent-selector",
                                type: "select",
                                options: [
                                    {
                                        "default": "Default"
                                    }
                                ]
                            }
                        ]
                    };

                    var languageField = {
                        type: "div",
                        class: "field datatype-field datatype-string",
                        html: [
                            {
                                type: "label",
                                html: "Language",
                                for: colName + "_lang"
                            },
                            {
                                name: colName + "_lang",
                                id: colName + "_lang",
                                type: "text",
                                placeholder: "e.g. en, fr, de-AT"
                            }
                        ]
                    };

                    var predDiv = {
                        "type": "div",
                        "class": "field",
                        id: colName + "_advanced",
                        "html": [parentField, predicateField, uriTemplateField, languageField]
                    };
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

            },

            _constructPreviewTabContent: function() {

                var ths = [];
                for (var i = 0; i < this.options.header.length; i++) {
                    var th = {
                        "type": "th",
                        "html": this.options.header[i]
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
                var originalRowCount = this.options.csvData.length - 1; // row 0 is header
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
                    var rowData = this.options.csvData[i];
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
            },

            _constructPublishOptionsCheckboxes: function() {

                var showOnHomepage = {
                    "type": "div",
                    "html": {
                        "type": "div",
                        "class": "ui checkbox",
                        "html": {
                            "type": "checkbox",
                            "name": "showOnHomepage",
                            "id": "showOnHomepage",
                            "caption": "Include my published dataset on DataDock search",
                            "checked": "checked"
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
                            "caption":
                                "Add to existing data if dataset already exists (default is to overwrite existing data)",
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
            },
            //end jquery.dform

            // Helper functions

            _getPrefix: function() {
                return this.options.publishUrl + "/" + this.options.ownerId + "/" + this.options.repoId;
            },

            _getIdentifierPrefix: function() {
                return this._getPrefix() + "/id/resource";
            },

            _slugify: function (original, whitespaceReplacement, specCharReplacement, casing) {
                var multiReplacementRegex = new RegExp("(" + whitespaceReplacement + "|" + specCharReplacement + ")+", "ig");
                var replacementTrimStart = new RegExp("^(" + whitespaceReplacement + "|" + specCharReplacement + ")+");
                var replacementTrimEnd = new RegExp("(" + whitespaceReplacement + "|" + specCharReplacement + ")+$");
                switch (casing) {
                case "lowercase":
                    var lowercase = original.replace(/\s+/g, whitespaceReplacement)
                        .replace(/[^A-Z0-9]+/ig, specCharReplacement)
                        .replace(multiReplacementRegex, whitespaceReplacement)
                        .replace(replacementTrimStart, "")
                        .replace(replacementTrimEnd, "")
                        .toLowerCase();
                    return lowercase;

                case "camelCase":
                    var camelCase = this._camelize(original);
                    return camelCase;

                default:
                    var slug = original.replace(/\s+/g, whitespaceReplacement)
                        .replace(/[^A-Z0-9]+/ig, specCharReplacement)
                        .replace(multiReplacementRegex, whitespaceReplacement)
                        .replace(replacementTrimStart, "")
                        .replace(replacementTrimEnd, "");
                    return slug;
                }
            },

            _camelize: function(str) {
                var camelised = str.split(/[\s_\-]/).map(function(word, index) {
                    if (index === 0) {
                        return word.toLowerCase();
                    }
                    return word.charAt(0).toUpperCase() + word.slice(1).toLowerCase();
                }).join("");
                var slug = camelised.replace(/[^A-Z0-9]+/ig, "");
                return slug;
            },

            _isArray: function(value) {
                return value && typeof value === 'object' && value.constructor === Array;
            },

            _sniffDatatype: function() {

            },

            _setDatatypesFromTemplate: function() {
                if (this.columnSet && this.options.templateMetadata) {
                    for (var i = 0; i < this.columnSet.length; i++) {
                        var colName = this.columnSet[i];
                        var colTemplate = schemaHelper.getColumnTemplate(this.options.templateMetadata, colName);
                        var colDatatype = schemaHelper.getColumnDatatype(colTemplate, "");
                        var colLang = schemaHelper.getColumnLang(colTemplate, "");
                        var selector = $("#" + colName + "_datatype");
                        if (colDatatype) {
                            if (selector) {
                                selector.val(colDatatype);
                            }
                        } else {
                            var valueUrl = schemaHelper.getColumnValueUrl(colTemplate, "");
                            if (valueUrl) {
                                if (/^\{[^\}]+\}$/.test(valueUrl)) {
                                    selector.val("uri");
                                } else {
                                    selector.val("uriTemplate");
                                    $('#' + colName + '_uriTemplate').val(valueUrl);
                                }
                            }
                        }
                        var aboutUrl = schemaHelper.getColumnAboutUrl(colTemplate, "");
                        if (aboutUrl) {
                            var parentColumn = schemaHelper.getColumnWithValueUrl(this.options.templateMetadata, aboutUrl);
                            if (parentColumn) {
                                $('#' + colName + '_parent').val(parentColumn);
                            }
                        }
                        if (colLang) {
                            $('#' + colName + '_lang').val(colLang);
                        }
                        this._updateDatatype(colName, selector.val());
                    }
                }
            },

            _setDatatypesFromSniffer: function() {
                if (this.columnSet && !this.options.templateMetadata) {
                    var sniffedDatatypes = datatypeSniffer.getDatatypes(this.options.csvData);
                    for (var i = 0; i < this.columnSet.length && i < sniffedDatatypes.length; i++) {
                        var colName = this.columnSet[i];
                        var colDatatype = sniffedDatatypes[i].type;
                        if (colDatatype === "float") colDatatype = "string"; // Float types are not handled in the UI yet
                        if (colDatatype) {
                            var selector = $("#" + colName + "_datatype");
                            if (selector) {
                                selector.val(colDatatype);
                            }
                        }
                    }
                }
            },

            // End Helper functions

            // CSVW Metadata builder
            _constructCsvwMetadata: function() {
                var csvw = {};
                var keywords = $("#keywords").val();

                csvw["@context"] = "http://www.w3.org/ns/csvw";
                csvw["url"] = $("#datasetId").val();
                csvw["dc:title"] = $("#datasetTitle").val();
                csvw["dc:description"] = $("#datasetDescription").val();

                if (keywords) {
                    if (keywords.indexOf(",") < 0) {
                        csvw["dcat:keyword"] = [keywords];
                    } else {
                        var keywordsArray = keywords.split(",");
                        csvw["dcat:keyword"] = keywordsArray;
                    }
                }

                csvw["dc:license"] = $("#datasetLicense").val();
                csvw["aboutUrl"] = $('#aboutUrlSuffix').val();
                csvw["tableSchema"] = this._constructCsvwTableSchema();

                console.log(csvw);
                return csvw;
            },

            _constructCsvwTableSchema: function() {
                var tableSchema = {};
                var columns = [];
                if (this.columnSet) {
                    for (var i = 0; i < this.columnSet.length; i++) {
                        var colName = this.columnSet[i];
                        var colId = "#" + colName;
                        var skip = $(colId + "_suppress").prop("checked");
                        var col = this._constructCsvwColumn(colName, skip);
                        columns.push(col);
                    }
                }
                tableSchema["columns"] = columns;
                return tableSchema;
            },

            _constructCsvwColumn: function(columnName, skip) {
                var colId = "#" + columnName;
                var datatype = $(colId + "_datatype").val();
                var column = {};
                column["name"] = columnName;
                if (skip) {
                    column["suppressOutput"] = true;
                } else {
                    var columnTitle = $(colId + "_title").val();
                    column["titles"] = [columnTitle];
                    var parentSelector = $(colId + "_parent");
                    var parent = parentSelector.val();
                    if (parent !== "default") {
                        var parentType = $("#" + parent + "_datatype").val();
                        if (parentType === "uri") {
                            column["aboutUrl"] = "{" + parent + "}";
                        } else if (parentType == "uriTemplate") {
                            var parentUriTemplate = $("#" + parent + "_uriTemplate").val();
                            if (parentUriTemplate) {
                                column["aboutUrl"] = parentUriTemplate;
                            } else {
                                column["aboutUrl"] = "{" + parent + "}";
                            }
                        }
                    }
                    if (datatype === "uriTemplate") {
                        var uriTemplate = $(colId + "_uriTemplate").val();
                        column["valueUrl"] = uriTemplate;
                    } else if (datatype === "uri") {
                        column["valueUrl"] = "{" + columnName + "}";
                    } else {
                        column["datatype"] = $(colId + "_datatype").val();
                    }
                    if (datatype === "string") {
                        var lang = $(colId + "_lang").val();
                        if (lang) {
                            column["lang"] = lang;
                        }
                    }
                    column["propertyUrl"] = $(colId + "_property_url").val();
                }
                return column;
            }
            // End CSVW Metadata builder
        });
})(jQuery);