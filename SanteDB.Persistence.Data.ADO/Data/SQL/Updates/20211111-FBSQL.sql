﻿/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20211111-01" name="Update:20211111-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Add PURGED status key</summary>
 *	<remarks>Adds status key PURGED to the database</remarks>
 *	<isInstalled>select ck_patch('20211111-01') from rdb$database</isInstalled>
 * </feature>
 */

INSERT INTO CD_TBL (CD_ID, IS_SYS) VALUES (char_to_uuid('0BBEC253-21A1-49CB-B376-7FE4D0592CDA'), true);--#!
INSERT INTO CD_VRSN_TBL (CD_VRSN_ID, CD_ID, STS_CD_ID, CLS_ID, CRT_PROV_ID, CRT_UTC, MNEMONIC) VALUES (gen_uuid(), char_to_uuid('0BBEC253-21A1-49CB-B376-7FE4D0592CDA'), char_to_uuid('C8064CBD-FA06-4530-B430-1A52F1530C27'), char_to_uuid('54B93182-FC19-47A2-82C6-089FD70A4F45'), char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'), CURRENT_TIMESTAMP, 'INACTIVE');--#!
INSERT INTO CD_NAME_TBL (NAME_ID, CD_ID, EFFT_VRSN_SEQ_ID, LANG_CS, VAL) 
	SELECT gen_uuid(), char_to_uuid('0BBEC253-21A1-49CB-B376-7FE4D0592CDA'), vrsn_seq_id, 'EN', 'Inactive'
	FROM CD_VRSN_TBL WHERE CD_ID = char_to_uuid('0BBEC253-21A1-49CB-B376-7FE4D0592CDA');--#!
INSERT INTO CD_SET_MEM_ASSOC_TBL  (CD_ID, SET_ID) VALUES (char_to_uuid('0BBEC253-21A1-49CB-B376-7FE4D0592CDA'), char_to_uuid('93A48F6A-6808-4C70-83A2-D02178C2A883'));--#!
INSERT INTO CD_SET_MEM_ASSOC_TBL  (CD_ID, SET_ID) VALUES (char_to_uuid('0BBEC253-21A1-49CB-B376-7FE4D0592CDA'), char_to_uuid('AAE906AA-27B3-4CDB-AFF1-F08B0FD31E59'));--#!
INSERT INTO CD_SET_MEM_ASSOC_TBL  (CD_ID, SET_ID) VALUES (char_to_uuid('0BBEC253-21A1-49CB-B376-7FE4D0592CDA'), char_to_uuid('C7578340-A8FF-4D7D-8105-581016324E68'));--#!

SELECT REG_PATCH('20211111-01') FROM RDB$DATABASE; --#!
