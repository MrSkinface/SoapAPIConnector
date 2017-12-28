1.1.2 (2017-10-02)

FIXES
 
* added throwing exception to sendDocApi (controller)
 
FEATURES
 
* added case for Metro : if there is exception [Not find successful Aperak for document invoice] => file will wait for next sending

1.1.3 (2017-10-02)

FIXES
 
* NONE
 
FEATURES
 
* added outgoing and incoming condra

1.1.4 (2017-10-04)

FIXES
 
* NONE
 
FEATURES
 
* added extended log for error while sending (xml not well-formed)
* added saving *POK tickets without signing (in timeline-mode)

1.1.4.1 (2017-10-05)

FIXES
 
* NONE
 
FEATURES
 
* defining custom sgn ext. in method (dfshelper)
* added custom sign extension to incoming condra

1.1.4.11 (2017-10-10)

FIXES
 
* added waiting for next try-send when not authorized error ocured
 
FEATURES
 
* NONE

1.2.0 (2017-10-19)

FIXES
 
* 
 
FEATURES
 
* removed all timeline dealing
* added zipping complete chains

1.2.1 (2017-10-24)

FIXES
 
* fixed double extension in chain container
* fixed base64 un-encoded xml in chain container
 
FEATURES
 
*

1.2.2 (2017-10-24)

FIXES
 
*

FEATURES
 
* added 'useSubFolders' to chain container' config

1.2.3 (2017-10-26)

FIXES
 
*

FEATURES
 
* added signs extensions to chain container
* added signs encoding (bool [isBase64] => base64 or raw/binary ) 

1.2.4 (2017-12-04)

FIXES
 
*

FEATURES
 
* added TORG1/TORG2 sign/confirm
* added xml status files creating for UPD/UKD, PDPOL, IZVPOL, UVUTOCH, POK

2.0.0alpha (2017-12-22)

FIXES
 
* FULL REFACTORING

FEATURES
 
* SOAP as web service link

2.0.0beta (2017-12-28)

FIXES
 
* change getting body from soap to rest
* optimized some internal method calls

FEATURES
 
*