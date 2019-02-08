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

    this.getAboutUrl = function(metadata) {
        return getPropertyValue(metadata, "aboutUrl", null);
    };

    this.getColumnTitle = function(colTemplate, defaultValue) {
        var titles = getPropertyValue(colTemplate, "titles", null);
        if (titles) {
            return (Array.isArray(titles)) ? titles[0] : titles;
        }
        return defaultValue;
    }

    this.getColumnPropertyUrl = function(colTemplate, defaultValue) {
        return getPropertyValue(colTemplate, "propertyUrl", defaultValue);
    }

    this.getColumnDatatype = function(colTemplate, defaultValue) {
        return getPropertyValue(colTemplate, "datatype", defaultValue);
    }

    this.getColumnSuppressed = function(colTemplate) {
        return getPropertyValue(colTemplate, "suppressOutput", false);
    }

}).apply(schemaHelper);
