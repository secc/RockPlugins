var beepSound = new Audio('../../../Assets/Sounds/beep.mp3');
var lastBarcode = "";

var startBarcode = function () {
    var App = {
        init : function() {
            var self = this;

            Quagga.init(this.state, function(err) {
                if (err) {
                    return self.handleError(err);
                }
                Quagga.start();
            });
        },
        handleError: function(err) {
            console.log(err);
        },
        _accessByPath: function(obj, path, val) {
            var parts = path.split('.'),
                depth = parts.length,
                setter = (typeof val !== "undefined") ? true : false;

            return parts.reduce(function(o, key, i) {
                if (setter && (i + 1) === depth) {
                    o[key] = val;
                }
                return key in o ? o[key] : {};
            }, obj);
        },
        _convertNameToState: function(name) {
            return name.replace("_", ".").split("-").reduce(function(result, value) {
                return result + value.charAt(0).toUpperCase() + value.substring(1);
            });
        },
        setState: function(path, value) {
            var self = this;

            if (typeof self._accessByPath(self.inputMapper, path) === "function") {
                value = self._accessByPath(self.inputMapper, path)(value);
            }

            self._accessByPath(self.state, path, value);

            console.log(JSON.stringify(self.state));
            App.detachListeners();
            Quagga.stop();
            App.init();
        },
        inputMapper: {
            inputStream: {
                constraints: function(value){
                    var values = value.split('x');
                    return {
                        width: parseInt(values[0]),
                        height: parseInt(values[1]),
                        facing: "environment"
                    }
                }
            },
            numOfWorkers: function(value) {
                return parseInt(value);
            },
            decoder: {
                readers: function(value) {
                    return [value + "_reader"];
                }
            }
        },
        state: {
            inputStream: {
                type : "LiveStream",
                constraints: {
                    //width: w,
                    //height: h,
                    facing: "environment" // or user
                }
            },
            locator: {
                patchSize: "medium",
                halfSample: true
            },
            numOfWorkers: 4,
            decoder: {
                readers : [ "code_128_reader"]
            },
            locate: true
        },
        lastResult : null
    };

    App.init();

    Quagga.onDetected(function(result) {
        var code = result.codeResult.code;
        console.log(code);
        //check for repeats
        if (code == lastBarcode || code.length!=6) {
            return;
        }
        lastBarcode = code;

        //test to make sure we read the barcode properly by matching the first and last halfs
        //eg: code "ABC" will have the barcode "ABCABC"
        if (code.slice(0, 3) != code.slice(3, 6)) {
            return;
        }

        //split the barcode in half for the attendance code
        code = code.slice(0, 3);

        beepSound.play();
        if ("vibrate" in navigator) {
            navigator.vibrate = navigator.vibrate || navigator.webkitVibrate || navigator.mozVibrate || navigator.msVibrate;

            if (navigator.vibrate) {
                navigator.vibrate([100,50,100]);
            } 
        }
        setTimeout(function () { sendPostBack(code); }, 1);

    });
}

var enableBarcode = function () {
    $('#BarcodeActual').show();
    $('#mainDiv').hide();
    startBarcode()
}

var disableBarcode = function () {
    $('#BarcodeActual').hide();
    $('#mainDiv').show();
    Quagga.stop()
}