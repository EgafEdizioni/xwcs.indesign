﻿/******************************************************************************
 * Copyright (C) 2016-2017 0ics srls <mail{at}0ics.it>
 * 
 * This file is part of xwcs libraries
 * xwcs libraries and all his part can not be copied 
 * and/or distributed without the express permission 
 * of 0ics srls
 *
 ******************************************************************************/
#targetengine "MB_BridgeRTF"
var arguments = [
    'indesign.log',
    (function () {
        var f;
        try{ f=_indesign.activeScript; }
        catch(_){ f=File(_.fileName); }
        //alert(f.parent + '/');
        return f.parent + '/';
    })(),
    "ASCII",
    300
];
#include "id.js"