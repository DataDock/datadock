/*
 * Contains widgets for use on account/user setting pages
 */

(function($) {

    $.widget("dd.searchButtonsEditor",
        {
            options: {
                value: ""
            },

            _create: function() {
                var self = this;
                self.element.addClass("table-editable");
                self.table = $("<table class='ui celled table'></table>");
                self.element.append(self.table);
                self.addButton = $("<button class='ui primary button'><i class='plus icon'></i>Add</button>");
                self._on(self.addButton,
                    {
                        "click": function (event) {
                            try {
                                self._newItem();
                            } finally {
                                event.preventDefault();
                            }
                        }
                    });
                self._on(self.table,
                    {
                        "click .table-remove": function (event) {
                            try {
                                self._removeRow(event.target);
                            } finally {
                                event.preventDefault();
                            }
                        },
                        "click .table-up": function (event) {
                            try {
                                self._moveRowUp(event.target);
                            } finally {
                                event.preventDefault();
                            }
                        },
                        "click .table-down": function (event) {
                            try {
                                self._moveRowDown(event.target);
                            } finally {
                                event.preventDefault();
                            }
                        }
                    });
                self.element.append(self.addButton);
                /*
                self.testButton = $("<button class='ui primary button'><i class='plus icon'></i>Test</button>");
                self._on(self.testButton,
                    {
                        "click": function (event) {
                            console.log(this.export());
                            event.preventDefault();
                        }
                    });
                self.element.append(self.testButton);
                */
                self.refresh();
            },

            refresh: function () {
                var self = this;
                self.table.empty();
                self.table.append($("<thead><th>Search Tag</th><th>Button Label</th><th>Remove</th><th>Reorder</th></thead>"));
                self.tableBody = $("<tbody></tbody>");
                self.table.append(self.tableBody);
                this.options.value.split(",").forEach((x, i) => {
                    var tagOptions = x.split(";");
                    var tag = tagOptions[0];
                    var label = tagOptions.length === 1 ? "" : tagOptions[1];
                    self._addRow(tag, label);
                });
            },

            export: function () {
                var value = "";
                var rows = this.tableBody.find("tr");
                rows.each((rowIx) => {
                    var row = $(rows[rowIx]);
                    var cells = row.find("td");
                    var tag = $(cells[0]).text().trim();
                    var label = $(cells[1]).text().trim();
                    if (tag) {
                        if (value) value += ",";
                        if (label) {
                            value += tag + ";" + label;
                        } else {
                            value += tag;
                        }
                    }
                });
                return value;
            },

            _addRow(tag, label) {
                var row = $("<tr></tr>");
                $("<td contenteditable='true'></td>").append(tag).appendTo(row);
                $("<td contenteditable='true'></td>").append(label).appendTo(row);
                $("<td class='collapsing'> <i class='table-remove large red x icon'></i></td>").appendTo(row);
                $("<td class='collapsing'></td>")
                    .append($("<i class='table-up large blue arrow up icon'></i>"))
                    .append($("<i class='table-down large blue arrow down icon'></i>"))
                    .appendTo(row);
                row.appendTo(this.tableBody).fadeIn();
            },

            _removeRow(target) {
                $(target).parents("tr").fadeOut().remove();
            },

            _moveRowUp(target) {
                var row = $(target).parents("tr");
                if (row.index() === 0) return;
                row.prev().before(row.get(0));
            },

            _moveRowDown(target) {
                var row = $(target).parents("tr");
                row.next().after(row.get(0));
            },

            _newItem: function () {
                this._addRow("tag", "");
                return false;
            }
        });
})(jQuery);