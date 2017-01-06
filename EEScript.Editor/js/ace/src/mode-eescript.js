define("ace/mode/eescript_highlight_rules",["require","exports","module","ace/lib/oop","ace/mode/text_highlight_rules"], function(require, exports, module) {
"use strict";

var oop = require("../lib/oop");
var TextHighlightRules = require("./text_highlight_rules").TextHighlightRules;

var EEScriptHighlightRules = function() {

    this.$rules = {
        "start" : [
            {
                token : "keyword",
                regex : /\([0-9]{1}\:[0-9]{1,2147483647}\)/
            },
            {
                regex: /\{(.*?)\}/,
                token: "string"
            },
            {
                token: "constant.numeric",
                regex: /[-+]?([0-9]*\.[0-9]+|[0-9]+)/
            },
            {
                token: "keyword.operator",
                regex: /\~[\ba-zA-Z\d\D][\ba-zA-Z\d_]*/
            },
            {
                token: "keyword.operator",
                regex: /\%[\ba-zA-Z\d\D][\ba-zA-Z\d_]*/
            },
            {
                token: "comment",
                regex: /\*.*/
            }
        ]
    };
    
    this.normalizeRules();
};

oop.inherits(EEScriptHighlightRules, TextHighlightRules);

exports.EEScriptHighlightRules = EEScriptHighlightRules;
});

define("ace/mode/eescript",["require","exports","module","ace/lib/oop","ace/mode/text","ace/mode/eescript_highlight_rules"], function(require, exports, module) {
"use strict";

var oop = require("../lib/oop");
var TextMode = require("./text").Mode;
var EEScriptHighlightRules = require("./eescript_highlight_rules").EEScriptHighlightRules;

var Mode = function() {
    this.HighlightRules = EEScriptHighlightRules;
    this.$behaviour = this.$defaultBehaviour;
};

oop.inherits(Mode, TextMode);

(function() {
    this.$id = "ace/mode/eescript";

    this.getNextLineIndent = function(state, line, tab) {
        return '';
    };
}).call(Mode.prototype);

exports.Mode = Mode;
});
