var schemaHelper = {};

(function() {
    var getPropertyValue = function(obj, propertyName, defaultValue) {
        return (obj && obj.hasOwnProperty(propertyName)) ? obj[propertyName] : defaultValue;
    };

    this.getDatasetId = function(metadata, defaultValue) {
        return getPropertyValue(metadata, "url", defaultValue);
    };

    this.getTitle = function(metadata, defaultValue) {
        return getPropertyValue(metadata, "dc:title", defaultValue);
    }

    this.getDescription = function(metadata, defaultValue) {
        return getPropertyValue(metadata, "dc:description", defaultValue);
    }

    this.getLicenseUri = function(metadata, defaultValue) {
        return getPropertyValue(metadata, "dc:license", defaultValue);
    }

    this.getTags = function(metadata) {
        var tags = getPropertyValue(metadata, "dcat:keyword", []);
        if (!Array.isArray(tags)) tags = [tags];
        return tags.join();
    }

    this.getColumnTemplate = function(metadata, columnName) {
        var tableSchema = getPropertyValue(metadata, "tableSchema", null);
        if (tableSchema) {
            var columns = getPropertyValue(tableSchema, "columns", null);
            if (columns) {
                var col = {};
                for (var i = 0; i < columns.length; i++) {
                    if (columns[i].name === columnName) {
                        col = columns[i];
                        break;
                    }
                }
                return col || {};
            }
        }
        return {};
    };

    this.getColumnWithValueUrl = function(metadata, valueUrl) {
        var tableSchema = getPropertyValue(metadata, "tableSchema", null);
        if (tableSchema) {
            var columns = getPropertyValue(tableSchema, "columns", null);
            if (columns) {
                for (var i = 0; i < columns.length; i++) {
                    if (Object.hasOwnProperty(columns[i], "valueUrl") && columns[i]["valueUrl"] === valueUrl) {
                        return columns[i]["name"];
                    }
                }
            }
        }
        return null;
    }

    this.getAboutUrl = function(metadata) {
        var tableSchema = getPropertyValue(metadata, "tableSchema", null);
        return getPropertyValue(tableSchema, "aboutUrl", null);
    };

    this.getColumnTitle = function(colTemplate, defaultValue) {
        var titles = getPropertyValue(colTemplate, "titles", null);
        if (titles) {
            return (Array.isArray(titles)) ? titles[0] : titles;
        }
        return defaultValue;
    }

    this.getColumnAboutUrl = function(colTemplate, defaultValue) {
        return getPropertyValue(colTemplate, "aboutUrl", defaultValue);
    }

    this.getColumnPropertyUrl = function(colTemplate, defaultValue) {
        return getPropertyValue(colTemplate, "propertyUrl", defaultValue);
    }

    this.getColumnDatatype = function(colTemplate, defaultValue) {
        return getPropertyValue(colTemplate, "datatype", defaultValue);
    }

    this.getColumnValueUrl = function(colTemplate, defaultValue) {
        return getPropertyValue(colTemplate, "valueUrl", defaultValue);
    }

    this.getColumnSuppressed = function(colTemplate) {
        return getPropertyValue(colTemplate, "suppressOutput", false);
    }

    this.getColumnLang = function(colTemplate, defaultValue) {
        return getPropertyValue(colTemplate, "lang", defaultValue);
    }
    
    this.makeAbsolute = function (tok, baseUri) {
        if (Array.isArray(tok)) {
            tok.forEach((item) => { schemaHelper.makeAbsolute(item, baseUri) });
        } else if (typeof (tok) === "object" && tok !== null) {
            Object.keys(tok).forEach((k) => {
                if (tok.hasOwnProperty(k)) {
                    if (k === "aboutUrl" ||
                        k === "propertyUrl" ||
                        k === "valueUrl") {
                        if (!tok[k].includes("://") && !/^\{[^\}]+\}$/.test(tok[k])) {
                            tok[k] = baseUri + tok[k];
                        }
                    } else {
                        if (tok[k] != null && (Array.isArray(tok[k]) || typeof(tok[k]) === "object")) {
                            schemaHelper.makeAbsolute(tok[k], baseUri);
                        }
                    }
                }
            });
        }
    }

}).apply(schemaHelper);
