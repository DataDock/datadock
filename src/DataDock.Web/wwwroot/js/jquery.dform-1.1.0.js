/*
 * jQuery dform plugin
 * Copyright (C) 2012 David Luecke <daff@neyeon.com>, [http://daffl.github.com/jquery.dform]
 * 
 * Licensed under the MIT license
 */
(function ($) {
	var _subscriptions = {},
		_types = {},
		each = $.each,
		addToObject = function (obj) {
			var result = function (data, fn, condition) {
				if (typeof data === 'object') {
					$.each(data, function (name, val) {
						result(name, val, condition);
					});
				} else if (condition === undefined || condition === true) {
					if (!obj[data]) {
						obj[data] = [];
					}
					obj[data].push(fn);
				}
			}
			return result;
		},
		isArray = $.isArray,
		/**
		 * Returns an array of keys (properties) contained in the given object.
		 *
		 * @param {Object} object The object to use
		 * @return {Array} An array containing all properties in the object
		 */
		keyset = function (object) {
			return $.map(object, function (val, key) {
				return key;
			});
		},
		/**
		 * Returns an object that contains all values from the given
		 * object that have a key which is also in the array keys.
		 *
		 * @param {Object} object The object to traverse
		 * @param {Array} keys The keys the new object should contain
		 * @return {Object} A new object containing only the properties
		 * with names given in keys
		 */
		withKeys = function (object, keys) {
			var result = {};
			each(keys, function (index, value) {
				if (object[value]) {
					result[value] = object[value];
				}
			});
			return result;
		},
		/**
		 * Returns an object that contains all value from the given
		 * object that do not have a key which is also in the array keys.
		 *
		 * @param {Object} object The object to traverse
		 * @param {Array} keys A list of keys that should not be contained in the new object
		 * @return {Object} A new object with all properties of the given object, except
		 * for the ones given in the list of keys
		 */
		withoutKeys = function (object, keys) {
			var result = {};
			each(object, function (index, value) {
				if (!~$.inArray(index, keys)) {
					result[index] = value;
				}
			});
			return result;
		},
		/**
		 * Run all subscriptions with the given name and options
		 * on an element.
		 *
		 * @param {String} name The name of the subscriber function
		 * @param {Object} options ptions for the function
		 * @param {String} type The type of the current element as in the registered types
		 * @return {Object} The jQuery object
		 */
		runSubscription = function (name, options, type) {
			if ($.dform.hasSubscription(name)) {
				this.each(function () {
					var element = $(this);
					each(_subscriptions[name], function (i, sfn) {
						// run subscriber function with options
						sfn.call(element, options, type);
					});
				});
			}
			return this;
		},
		/**
		 * Run all subscription functions with given options.
		 *
		 * @param {Object} options The options to use
		 * @return {Object} The jQuery element this function has been called on
		 */
		runAll = function (options) {
			var type = options.type, self = this;
			// Run preprocessing subscribers
			this.dform('run', '[pre]', options, type);
			each(options, function (name, sopts) {
				self.dform('run', name, sopts, type);
			});
			// Run post processing subscribers
			this.dform('run', '[post]', options, type);
			return this;
		};

	/**
	 * Globals added directly to the jQuery object
	 */
	$.extend($, {
		keyset : keyset,
		withKeys : withKeys,
		withoutKeys : withoutKeys,
		dform : {
			/**
			 * Default options the plugin is initialized with:
			 *
			 * ## prefix
			 *
			 * The Default prefix used for element classnames generated by the dform plugin.
			 * Defaults to _ui-dform-_
			 * E.g. an element with type text will have the class ui-dform-text
			 *
			 */
			options : {
				prefix : "ui-dform-"
			},

			/**
			 * A function that is called, when no registered type has been found.
			 * The default behaviour returns an HTML element with the tag
			 * as specified in type and the HTML attributes given in options
			 * (without subscriber options).
			 *
			 * @param {Object} options
			 * @return {Object} The created object
			 */
			defaultType : function (options) {
				return $("<" + options.type + ">").dform('attr', options);
			},
			/**
			 * Return all types.
			 *
			 * @params {String} name (optional) If passed return
			 * all type generators for a given name.
			 * @return {Object} Mapping from type name to
			 * an array of generator functions.
			 */
			types : function (name) {
				return name ? _types[name ] : _types;
			},
			/**
			 * Register an element type function.
			 *
			 * @param {String|Array} data Can either be the name of the type
			 * function or an object that contains name : type function pairs
			 * @param {Function} fn The function that creates a new type element
			 */
			addType : addToObject(_types),
			/**
			 * Returns all subscribers or all subscribers for a given name.
			 *
			 * @params {String} name (optional) If passed return all
			 * subscribers for a given name
			 * @return {Object} Mapping from subscriber names
			 * to an array of subscriber functions.
			 */
			subscribers : function (name) {
				return name ? _subscriptions[name] : _subscriptions;
			},
			/**
			 * Register a subscriber function.
			 *
			 * @param {String|Object} data Can either be the name of the subscriber
			 * function or an object that contains name : subscriber function pairs
			 * @param {Function} fn The function to subscribe or nothing if an object is passed for data
			 * @param {Array} deps An optional list of dependencies
			 */
			subscribe : addToObject(_subscriptions),
			/**
			 * Returns if a subscriber function with the given name
			 * has been registered.
			 *
			 * @param {String} name The subscriber name
			 * @return {Boolean} True if the given name has at least one subscriber registered,
			 *     false otherwise
			 */
			hasSubscription : function (name) {
				return _subscriptions[name] ? true : false;
			},
			/**
			 * Create a new element.
			 *
			 * @param {Object} options - The options to use
			 * @return {Object} The element as created by the builder function specified
			 *     or returned by the defaultType function.
			 */
			createElement : function (options) {
				if (!options.type) {
					throw "No element type given! Must always exist.";
				}
				var type = options.type,
					element = null,
				// We don't need the type key in the options
					opts = $.withoutKeys(options, ["type"]);

				if (_types[type]) {
					// Run all type element builder functions called typename
					each(_types[type], function (i, sfn) {
						element = sfn.call(element, opts);
					});
				} else {
					// Call defaultType function if no type was found
					element = $.dform.defaultType(options);
				}
				return $(element);
			},
			methods : {
				/**
				 * Run all subscriptions with the given name and options
				 * on an element.
				 *
				 * @param {String} name The name of the subscriber function
				 * @param {Object} options ptions for the function
				 * @param {String} type The type of the current element as in the registered types
				 * @return {Object} The jQuery object
				 */
				run : function (name, options, type) {
					if (typeof name !== 'string') {
						return runAll.call(this, name);
					}
					return runSubscription.call(this, name, options, type);
				},
				/**
				 * Creates a form element on an element with given options
				 *
				 * @param {Object} options The options to use
				 * @return {Object} The jQuery element this function has been called on
				 */
				append : function (options, converter) {
					if (converter && $.dform.converters && $.isFunction($.dform.converters[converter])) {
						options = $.dform.converters[converter](options);
					}
					// Create element (run builder function for type)
					var element = $.dform.createElement(options);
					this.append(element);
					// Run all subscriptions
					element.dform('run', options);
				},
				/**
				 * Adds HTML attributes to the current element from the given options.
				 * Any subscriber will be omitted so that the attributes will contain any
				 * key value pair where the key is not the name of a subscriber function
				 * and is not in the string array excludes.
				 *
				 * @param {Object} object The attribute object
				 * @param {Array} excludes A list of keys that should also be excluded
				 * @return {Object} The jQuery object of the this reference
				 */
				attr : function (object, excludes) {
					// Ignore any subscriber name and the objects given in excludes
					var ignores = $.keyset(_subscriptions);
					isArray(excludes) && $.merge(ignores, excludes);
					this.attr($.withoutKeys(object, ignores));
				},
				/**
				 *
				 *
				 * @param params
				 * @param success
				 * @param error
				 */
				ajax : function (params, success, error) {
					var options = {
						error : error,
						url : params
					}, self = this;
					if (typeof params !== 'string') {
						$.extend(options, params);
					}
					options.success = function (data) {
						var callback = success || params.success;
						self.dform(data);
						if(callback) {
							callback.call(self, data);
						}
					}
					$.ajax(options);
				},
				/**
				 *
				 *
				 * @param options
				 */
				init : function (options, converter) {
					var opts = options.type ? options : $.extend({ "type" : "form" }, options);
					if (converter && $.dform.converters && $.isFunction($.dform.converters[converter])) {
						opts = $.dform.converters[converter](opts);
					}
					if (this.is(opts.type)) {
						this.dform('attr', opts);
						this.dform('run', opts);
					} else {
						this.dform('append', opts);
					}
				}
			}
		}
	});

	/**
	 * The jQuery plugin function
	 *
	 * @param options The form options
	 * @param {String} converter The name of the converter in $.dform.converters
	 * that will be used to convert the options
	 */
	$.fn.dform = function (options, converter, error) {
		var self = $(this);
		if ($.dform.methods[options]) {
			$.dform.methods[options].apply(self, Array.prototype.slice.call(arguments, 1));
		} else {
			if (typeof options === 'string') {
				$.dform.methods.ajax.call(self, {
					url : options,
					dataType : 'json'
				}, converter, error);
			} else {
				$.dform.methods.init.apply(self, arguments);
			}
		}
		return this;
	}
})(jQuery);

/*
 * jQuery dform plugin
 * Copyright (C) 2012 David Luecke <daff@neyeon.com>, [http://daffl.github.com/jquery.dform]
 *
 * Licensed under the MIT license
 */
(function ($) {
	var each = $.each,
		_element = function (tag, excludes) {
			return function (ops) {
				return $(tag).dform('attr', ops, excludes);
			};
		},
		_html = function (options, type) {
			var self = this;
			if ($.isPlainObject(options)) {
				self.dform('append', options);
			} else if ($.isArray(options)) {
				each(options, function (index, nested) {
					self.dform('append', nested);
				});
			} else {
				self.html(options);
			}
		};

	$.dform.addType({
		container : _element("<div>"),
		text : _element('<input type="text" />'),
		password : _element('<input type="password" />'),
		submit : _element('<input type="submit" />'),
		reset : _element('<input type="reset" />'),
		hidden : _element('<input type="hidden" />'),
		radio : _element('<input type="radio" />'),
		checkbox : _element('<input type="checkbox" />'),
		file : _element('<input type="file" />'),
		number : _element('<input type="number" />'),
		url : _element('<input type="url" />'),
		tel : _element('<input type="tel" />'),
		email : _element('<input type="email" />'),
		checkboxes : _element("<div>", ["name"]),
		radiobuttons : _element("<div>", ["name"])
	});

	$.dform.subscribe({
		/**
		 * Adds a class to the current element.
		 * Ovverrides the default behaviour which would be replacing the class attribute.
		 *
		 * @param options A list of whitespace separated classnames
		 * @param type The type of the *this* element
		 */
		"class" : function (options, type) {
			this.addClass(options);
		},

		/**
		 * Sets html content of the current element
		 *
		 * @param options The html content to set as a string
		 * @param type The type of the *this* element
		 */
		"html" : _html,

		/**
		 * Recursively appends subelements to the current form element.
		 *
		 * @param options Either an object with key value pairs
		 *	 where the key is the element name and the value the
		 *	 subelement options or an array of objects where each object
		 *	 is the options for a subelement
		 * @param type The type of the *this* element
		 */
		"elements" : _html,

		/**
		 * Sets the value of the current element.
		 *
		 * @param options The value to set
		 * @param type The type of the *this* element
		 */
		"value" : function (options) {
			this.val(options);
		},

		/**
		 * Set CSS styles for the current element
		 *
		 * @param options The Styles to set
		 * @param type The type of the *this* element
		 */
		"css" : function (options) {
			this.css(options);
		},

		/**
		 * Adds options to select type elements or radio and checkbox list elements.
		 *
		 * @param options A key value pair where the key is the
		 *	 option value and the value the options text or the settings for the element.
		 * @param type The type of the *this* element
		 */
		"options" : function (options, type) {
			var self = this;
			// Options for select elements
			if ((type === "select" || type === "optgroup") && typeof options !== 'string')
			{
				each(options, function (value, content) {
					var option = { type : 'option', value : value };
					if (typeof (content) === "string") {
						option.html = content;
					}
					if (typeof (content) === "object") {
						option = $.extend(option, content);
					}
					self.dform('append', option);
				});
			}
			else if (type === "checkboxes" || type === "radiobuttons") {
				// Options for checkbox and radiobutton lists
				each(options, function (value, content) {
					var boxoptions = ((type === "radiobuttons") ? { "type" : "radio" } : { "type" : "checkbox" });
					if (typeof(content) === "string") {
						boxoptions["caption"] = content;
					} else {
						$.extend(boxoptions, content);
					}
					boxoptions["value"] = value;
					self.dform('append', boxoptions);
				});
			}
		},

		/**
		 * Adds caption to elements.
		 *
		 * Depending on the element type the following elements will
		 * be used:
		 * - A legend for <fieldset> elements
		 * - A <label> next to <radio> or <checkbox> elements
		 * - A <label> before any other element
		 *
		 * @param options A string for the caption or the options for the
		 * @param type The type of the *this* element
		 */
		"caption" : function (options, type) {
			var ops = {};
			if (typeof (options) === "string") {
				ops["html"] = options;
			} else {
				$.extend(ops, options);
			}

			if (type == "fieldset") {
				// Labels for fieldsets are legend
				ops.type = "legend";
				this.dform('append', ops);
			} else {
				ops.type = "label";
				if (this.attr("id")) {
					ops["for"] = this.attr("id");
				}
				var label = $($.dform.createElement(ops));
				if (type === "checkbox" || type === "radio") {
					this.parent().append($(label));
				} else {
					label.insertBefore(this);
				}
				label.dform('run', ops);
			}
		},

		/**
		 * The subscriber for the type parameter.
		 * Although the type parameter is used to get the correct element
		 * type it is just treated as a simple subscriber otherwise.
		 * Since every element needs a type
		 * parameter feel free to add other type subscribers to do
		 * any processing between [pre] and [post].
		 *
		 * This subscriber adds the auto generated classes according
		 * to the type prefix in $.dform.options.prefix.
		 *
		 * @param options The name of the type
		 * @param type The type of the *this* element
		 */
		"type" : function (options, type) {
			if ($.dform.options.prefix) {
				this.addClass($.dform.options.prefix + type);
			}
		},
		/**
		 * Retrieves JSON data from a URL and creates a sub form.
		 *
		 * @param options
		 * @param type
		 */
		"url" : function (options) {
			this.dform('ajax', options);
		},
		/**
		 * Post processing function, that will run whenever all other subscribers are finished.
		 *
		 * @param options All options that have been used for
		 * @param type The type of the *this* element
		 */
		"[post]" : function (options, type) {
			if (type === "checkboxes" || type === "radiobuttons") {
				var boxtype = ((type === "checkboxes") ? "checkbox" : "radio");
				this.children("[type=" + boxtype + "]").each(function () {
					$(this).attr("name", options.name);
				});
			}
		}
	});
})(jQuery);

/*
 * jQuery dform plugin
 * Copyright (C) 2012 David Luecke <daff@neyeon.com>, [http://daffl.github.com/jquery.dform]
 * 
 * Licensed under the MIT license
 */
(function($)
{
	var _getOptions = function(type, options)
		{
			return $.withKeys(options, $.keyset($.ui[type]["prototype"]["options"]));
		},
		_get = function(keys, obj) {
			for(var item = obj, i = 0; i < keys.length; i++) {
				item = item[keys[i]];
				if(!item) {
					return null;
				}
			}
			return item;
		}
		
	$.dform.addType("progressbar",
		/**
		 * Returns a jQuery UI progressbar.
		 *
		 * @param options  As specified in the jQuery UI progressbar documentation at
		 * 	http://jqueryui.com/demos/progressbar/
		 */
		function(options)
		{
			return $("<div>").dform('attr', options).progressbar(_getOptions("progressbar", options));
		}, $.isFunction($.fn.progressbar));

	$.dform.addType("slider",
		/**
		 * Returns a slider element.
		 *
		 * @param options As specified in the jQuery UI slider documentation at
		 * 	http://jqueryui.com/demos/slider/
		 */
		function(options)
		{
			return $("<div>").dform('attr', options).slider(_getOptions("slider", options));
		}, $.isFunction($.fn.slider));

	$.dform.addType("accordion",
		/**
		 * Creates an element container for a jQuery UI accordion.
		 *
		 * @param options As specified in the jQuery UI accordion documentation at
		 * 	http://jqueryui.com/demos/accordion/
		 */
		function(options)
		{
			return $("<div>").dform('attr', options);
		}, $.isFunction($.fn.accordion));

	$.dform.addType("tabs",
		/**
		 * Returns a container for jQuery UI tabs.
		 *
		 * @param options The options as in jQuery UI tab
		 */
		function(options)
		{
			return $("<div>").dform('attr', options);
		}, $.isFunction($.fn.tabs));
	
	$.dform.subscribe("entries",
		/**
		 *  Create entries for the accordion type.
		 *  Use the <elements> subscriber to create subelements in each entry.
		 *
		 * @param options All options for the container div. The <caption> will be
		 * 	turned into the accordion or tab title.
		 * @param type The type. This subscriber will only run for accordion
		 */
		function(options, type) {
			if(type == "accordion")
			{
				var scoper = this;
				$.each(options, function(index, options) {
					var el = $.extend({ "type" : "div" }, options);
					$(scoper).dform('append', el);
					if(options.caption) {
						var label = $(scoper).children("div:last").prev();
						label.replaceWith('<h3><a href="#">' + label.html() + '</a></h3>');
					}
				});
			}
		}, $.isFunction($.fn.accordion));

	$.dform.subscribe("entries",
		/**
		 *  Create entries for the accordion type.
		 *  Use the <elements> subscriber to create subelements in each entry.
		 *
		 * @param options All options for the container div. The <caption> will be
		 * 	turned into the accordion or tab title.
		 * @param type The type. This subscriber will only run for accordion
		 */
		function(options, type) {
			if(type == "tabs")
			{
				var scoper = this;
				this.append("<ul>");
				var ul = $(scoper).children("ul:first");
				$.each(options, function(index, options) {
					var id = options.id ? options.id : index;
					$.extend(options, { "type" : "container", "id" : id });
					$(scoper).dform('append', options);
					var label = $(scoper).children("div:last").prev();
					$(label).wrapInner($("<a>").attr("href", "#" + id));
					$(ul).append($("<li>").wrapInner(label));
				});
			}
		}, $.isFunction($.fn.tabs));
		
	$.dform.subscribe("dialog",
		/**
		 * Turns an element into a jQuery UI dialog.
		 *
		 * @param options As specified in the [jQuery UI dialog documentation\(http://jqueryui.com/demos/dialog/)
		 */
		function(options)
		{
			this.dialog(options);
		}, $.isFunction($.fn.dialog));

	$.dform.subscribe("resizable",
		/**
		 * Make the current element resizable.
		 *
		 * @param options As specified in the [jQuery UI resizable documentation](http://jqueryui.com/demos/resizable/)
		 */
		function(options)
		{
			this.resizable(options);
		}, $.isFunction($.fn.resizable));

	$.dform.subscribe("datepicker",
		/**
		 * Adds a jQuery UI datepicker to an element of type text.
		 *
		 * @param options As specified in the [jQuery UI datepicker documentation](http://jqueryui.com/demos/datepicker/)
		 * @param type The type of the element
		 */
		function(options, type)
		{
			if (type == "text") {
				this.datepicker(options);
			}
		}, $.isFunction($.fn.datepicker));

	$.dform.subscribe("autocomplete",
		/**
		 * Adds the autocomplete feature to a text element.
		 *
		 * @param options As specified in the [jQuery UI autotomplete documentation](http://jqueryui.com/demos/autotomplete/)
		 * @param type The type of the element
		 */
		function(options, type)
		{
			if (type == "text") {
				this.autocomplete(options);
			}
		}, $.isFunction($.fn.autocomplete));

	$.dform.subscribe("[post]",
		/**
		 * Post processing subscriber that adds jQuery UI styling classes to
		 * text, textarea, password and fieldset elements as well
		 * as calling .button() on submit or button elements.
		 *
		 * Additionally, accordion and tabs elements will be initialized
		 * with their options.
		 *
		 * @param options All options that have been passed for creating the element
		 * @param type The type of the element
		 */
		function(options, type)
		{
			if (this.parents("form").hasClass("ui-widget"))
			{
				if ((type === "button" || type === "submit") && $.isFunction($.fn.button)) {
					this.button();
				}
				if (!!~$.inArray(type, [ "text", "textarea", "password",
						"fieldset" ])) {
					this.addClass("ui-widget-content ui-corner-all");
				}
			}
			if(type === "accordion" || type === "tabs") {
				this[type](_getOptions(type, options));
			}
		});
	
	$.dform.subscribe("[pre]",
		/**
		 * Add a preprocessing subscriber that calls .validate() on the form,
		 * so that we can add rules to the input elements. Additionally
		 * the jQuery UI highlight classes will be added to the validation
		 * plugin default settings if the form has the ui-widget class.
		 * 
		 * @param options All options that have been used for
		 * creating the current element.
		 * @param type The type of the *this* element
		 */
		function(options, type)
		{
			if(type == "form")
			{
				var defaults = {};
				if(this.hasClass("ui-widget"))
				{
					defaults = {
						highlight: function(input)
						{
							$(input).addClass("ui-state-highlight");
						},
						unhighlight: function(input)
						{
							$(input).removeClass("ui-state-highlight");
						}
					};
                }
                if (this.is("#metadataEditorForm")) {
                    // DataDock specific jquery.validate configuration
                    defaults = {
                        debug: true,
                        ignore: ".skip-validation",
                        onkeyup: false,
                        onfocusout: false,
                        onclick: false,
                        showErrors: function (errorMap, errorList) {
                            console.log(errorMap);
                            console.log(errorList);
                            var numErrors = this.numberOfInvalids();
                            if (numErrors) {
                                var validationMessage = numErrors === 1
                                    ? '1 field is missing or invalid, please correct this before submitting your data.'
                                    : numErrors +
                                    ' fields are missing or invalid, please correct this before submitting your data.';

                                $("#validation-messages").html(validationMessage);
                                var invalidFieldList = $('<ul />');
                                $.each(errorList,
                                    function(i) {
                                        var error = errorList[i];
                                        $('<li/>')
                                            .addClass('invalid-field')
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
                       /* invalidHandler: function (event, validator) {
                            // 'this' refers to the form
                            var errors = validator.numberOfInvalids();
                            if (errors) {
                                var message = errors === 1
                                    ? 'You missed 1 field. It has been highlighted'
                                    : 'You missed ' + errors + ' fields. They have been highlighted';
                                $("#validation-messages").html(message);
                                $("#validation-messages").show();
                            } else {
                                $("#validation-messages").hide();
                            }
                        },*/
                        errorPlacement: function (error, element) {
                            error.insertBefore(element);
                        },
                        highlight: function(element, errorClass, validClass) {
                            $(element).parents(".field").addClass(errorClass);
                        },
                        unhighlight: function(element, errorClass, validClass) {
                            $(element).parents(".field").removeClass(errorClass);
                        },
                        submitHandler: function (e) {
                            sendData(e);
                        }
                };
			    }
				if (typeof (options.validate) == 'object') {
					$.extend(defaults, options.validate);
				}
				this.validate(defaults);
			}
		}, $.isFunction($.fn.validate));

		/**
		 * Adds support for the jQuery validation rulesets.
		 * For types: text, password, textarea, radio, checkbox sets up rules through rules("add", rules) for validation plugin
		 * For type <form> sets up as options object for validate method of validation plugin
		 * For rules of types checkboxes and radiobuttons you should use this subscriber for type form (to see example below)
		 *
		 * @param options
		 * @param type
		 */
	$.dform.subscribe("validate", function(options, type)
		{
			if (type != "form") {
				this.rules("add", options);
			}
		}, $.isFunction($.fn.validate));

	$.dform.subscribe("ajax",
		/**
		 * If the current element is a form, it will be turned into a dynamic form
		 * that can be submitted asynchronously.
		 *
		 * @param options Options as specified in the [jQuery Form plugin documentation](http://jquery.malsup.com/form/#options-object)
		 * @param type The type of the element
		 */
		function(options, type)
		{
			if(type === "form")
			{
				this.ajaxForm(options);
			}
		}, $.isFunction($.fn.ajaxForm));

	$.dform.subscribe('html',
		/**
		 * Extends the html subscriber that will replace any string with it's translated
		 * equivalent using the jQuery Global plugin. The html content will be interpreted
		 * as an index string where the first part indicates the localize main index and
		 * every following a sub index using getValueAt.
		 *
		 * @param options The dot separated html string to localize
		 * @param type The type of the this element
		 */
		function(options, type)
		{
			if(typeof options === 'string') {
				var keys = options.split('.'),
					translated = Globalize.localize(keys.shift());
				if(translated = _get(keys, translated)) {
					$(this).html(translated);
				}
			}
		}, typeof Globalize !== 'undefined' && $.isFunction(Globalize.localize));

	$.dform.subscribe('options',
		/**
		 * Extends the options subscriber for using internationalized option
		 * lists.
		 *
		 * @param options Options as specified in the <jQuery Form plugin documentation at http://jquery.malsup.com/form/#options-object>
		 * @param type The type of the element.
		 */
		function(options, type)
		{
			if(type === 'select' && typeof(options) === 'string') {
				$(this).html('');
				var keys = options.split('.'),
					optlist = Globalize.localize(keys.shift());
				if(optlist = _get(keys, optlist)) {
					$(this).dform('run', 'options', optlist, type);
				}
			}
		}, typeof Globalize !== 'undefined' && $.isFunction(Globalize.localize));
})(jQuery);
