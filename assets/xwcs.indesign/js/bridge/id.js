﻿/******************************************************************************
 * Copyright (C) 2016-2017 0ics srls <mail{at}0ics.it>
 * 
 * This file is part of xwcs libraries
 * xwcs libraries and all his part can not be copied 
 * and/or distributed without the express permission 
 * of 0ics srls
 *
 ******************************************************************************/
#target "indesign"
#targetengine "session_CsBridge"

#include "support.js"

#include "bridge.js"

#include "fileman.js"



 // activate new menus
(function(ind)
    // -------------------------------------
    // Install and/or update the menu/submenu and connect
    // the corresponding menu actions if script files are available
{
    var _indesign = ind;
    
    // Settings and constants
    // ---
    var MENU_NAME = "Egaf",
        FEATURES = [
            { caption: "Apri RTF...", fileName: "file.open.rtf.jsx", subName: "" },
            { caption: "Salva RTF", fileName: "file.save.rtf.jsx", subName: "" },
            { caption: "Salva RTF nuovo documento", fileName: "file.save.rtf.new.doc.jsx", subName: "" },
			{ separator: true, subName: "" },
			{ caption: "Inserisci tag figure/categorie...", fileName: "edit.insert.tags.figcat.js", subName: "" },
            { caption: "Laco", fileName: "laco.js", subName: "" }
        ],
        LO_END = LocationOptions.atEnd,
        INDESIGN_ROOT_MENU = _indesign.menus.item( '$ID/Main' ),
        FEATURE_LOCATION_PATH = (function()
        {
            if(CsBridge == undefined){
                alert("Failed CsBridge load!");
                return '';
            }else{
                return CsBridge.options().scriptPath + '/';
            }
        })();
 
    // (Re)set the actions
    // Note: checks also whether script files are available
    // ---
    var    t, f,
        i = FEATURES.length;
    while( i-- )
    {
        t = FEATURES[i];
        if( t.separator ) continue;
 
        if( (f=File(FEATURE_LOCATION_PATH + t.fileName)).exists )
        {
            CsBridge.log('Adding menu ... : ' + FEATURE_LOCATION_PATH + t.fileName);
            // The script file exists => create the corresponding action
            // and directly attach the event listener to the file
            // (no need to use _indesign.doScript(...) here)
            // ---
            (t.action = _indesign.scriptMenuActions.add( t.caption )).
                addEventListener('onInvoke', f);
        }
        else
        {
            CsBridge.log('Missing file for menu ... : ' + FEATURE_LOCATION_PATH + t.fileName);
            // The script file does not exist => remove that feature
            // ---
            FEATURES.splice(i,1);
        }
    }
 
    // ---
    // Create/reset the custom menu container *if necessary*
    // Note:  menus/submenus are application-persistent
    // ---
    var    mnu = INDESIGN_ROOT_MENU.submenus.itemByName( MENU_NAME );
    if( !mnu.isValid )
    {
        // Our custom menu hasn't been created yet
        // ---
        if( !FEATURES.length ) return;
        mnu = INDESIGN_ROOT_MENU.submenus.add(
            MENU_NAME,
            LocationOptions.after,
            INDESIGN_ROOT_MENU.submenus.item( '$ID/&Window' )
            );
    }
    else
    {
        // Our custom menu already exists, but we must clear
        // any sub element in order to rebuild a fresh structure
        // ---
        mnu.menuElements.everyItem().remove();
 
        // If FEATURES is empty, remove the menu itself
        // ---
        if( !FEATURES.length ){ mnu.remove(); return; }
    }
 
    // ---
    // Now, let's fill mnu with respect to FEATURES' order
    // (Possible submenus are specified in .subName and created on the fly)
    // ---
    var s,
        n = FEATURES.length,
        subs = {},
        sub = null;
    for( i=0 ; i < n ; ++i )
    {
        t = FEATURES[i];
 
        // Target the desired submenu
        // ---
        sub = (s=t.subName) ?
            ( subs[s] || (subs[s]=mnu.submenus.add( s, LO_END )) ) :
            mnu;
 
        // Connect the related action OR create a separator
        // ---
        if( t.separator )
            sub.menuSeparators.add( LO_END);
        else
            sub.menuItems.add( t.action, LO_END );
    }
})(app);