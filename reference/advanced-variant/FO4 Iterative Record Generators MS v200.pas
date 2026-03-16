{
FO4 Iterative Record Generators (Material Swaps / OMOD / COBJ) – v110

What this script is for
- Automates building and maintaining large, consistent sets of records for “material-swap / skin” style frameworks.
- Primary supported record families:
  - MSWP (Material Swaps)
  - OMOD (Object Modifications that reference MSWPs)
  - COBJ (Recipes for OMODs, and also “COBJ-only” recipes for created physical items)
  - MISC (Optional “miscmod_*” items when your project uses them)

Two high-level modes
1) BUILD+LABEL
   - Creates/updates the selected record families from your inputs, then applies FINAL in‑game labels (FULL fields, etc.)
2) REVERT+BUILD+LABEL
   - First reverts affected records back to their canonical “build format” (so regeneration is stable),
     then rebuilds, then applies labels again.
   - Use this for update/append workflows when you’re iterating on an existing set.

Inputs
A) label.csv (required)
   There are TWO label.csv formats, and the script auto-detects which one you’re using:

   1) OMOD-style label.csv (for MSWP/OMOD projects)
      - Uses ObjType/ObjName/Slot/Opt style columns (the “framework” format).

   2) COBJ-only label.csv (for created physical items)
      - Uses WORKBENCHNAME / CATNAME / ITEMNAME style rows to build craftable item recipes.
      - This path does NOT use OMODs, and the game cannot tolerate “free” created-item recipes.
        If components are blank for a created-item recipe, the default becomes 1x c_steel.

   NOTE: The script will show an error and loop back if your build toggles don’t match the detected label.csv format.

B) build.csv (optional)
   - Used for cross-product generation of Slot/Opt grids (advanced).
   - If build.csv is not provided but the current run requires build inputs, the script can prompt for a single-row “build input” via UI.

C) cobjrecipes.csv (optional, OMOD-style only)
   - Optional override layer for the COBJ recipes that are generated alongside OMODs.
   - Rules are layered from least->most specific; blank fields inherit from broader rules.
   - If there is no DEFAULT row and no rule matches, components are treated as FREE (no components) for OMOD-style recipes.

Build toggles (Options window)
- MSWP: Build/overwrite or Skip
- OMOD: Build/overwrite or Skip (requires MSWP)
- COBJ: Build/overwrite or Skip (can run without OMOD)
- MISC: Build/overwrite or Skip (requires BOTH OMOD + COBJ)

Normalization helpers (COBJ-only label.csv)
- WORKBENCHNAME is normalized to EditorIDs:
  - workbenchchemlab (chemlab / chemistry / lab / etc.)
  - workbenchcooking (cooking / cook / food / etc.)
- CATNAME is normalized to: recipe<catname>

}
unit FO4_CSV_Generator_Labeler;

// -----------------------------------------------------------------------------
// QUICK INDEX (Ctrl+F)
//   [UTIL] helpers: TrimOrEmpty, SafeInt, Pad2, ...
//   [CSV]  IO/parsing: LoadCSVWithDialog, GetCol, ParsePairLists, ...
//   [LBL]  label + pairs: LoadLabelCSVRequired, ResolvePairsCSVForMSWP, ...
//   [NAM]  naming/tokens: CanonicalObjSeg, ObjTypeTokenForEDID, ExpandTokens, ...
//   [REC]  record ops: EnsureKYWDInTarget, EnsureAndRebuildMSWP/OMOD/COBJ, ...
//   [UI]   dialogs: OptionsDialog, BuildInputDialog, UI_* scaling, ...
//   [RUN]  orchestration: RevertPass, BuildPass, LabelPass, Initialize
// -----------------------------------------------------------------------------
const
  verboseDebug = False;

  MODE_BUILD_LABEL        = 1;
  MODE_REVERT_BUILD_LABEL = 2;


  // Label CSV kind detection (used to suppress irrelevant UI)
  LBL_KIND_UNKNOWN    = 0;
  LBL_KIND_OMOD_LABEL = 1; // ObjType/ObjName/Slot/Opt style
  LBL_KIND_COBJ_ONLY  = 2; // WorkbenchName/CatName/ItemName style

  kywdAPPrefix = 'ap_';
  kywdMAPrefix = 'ma_';
  mswpPrefix   = 'mswp_';
  omodPrefix   = 'mod_';
  cobjPrefix   = 'co_';
  miscPrefix   = 'misc';

  fullGatePrefix = 'FULL_';

var
  gTargetFile: IInterface;
  gMode: integer; // 1=BUILD+LABEL, 2=REVERT+BUILD+LABEL
  gDoBuildOMOD: boolean;
  gDoBuildCOBJ: boolean;
  gDoBuildMISC: boolean;
  gDoBuildMSWP: boolean;
  // Workaround: JvInterpreter local-var symbol glitches in huge procedures
  gMSWPPairsBase: TStringList;
  gMSWPPairsDest: TStringList;
  gBuildBaseList: TStringList;
  gBuildDestList: TStringList;

  gBuildSlRow: TStringList;
  gUITextScalePct: integer;
  gDlgRGMSWP: TRadioGroup;
  gDlgRGOMOD: TRadioGroup;
  gDlgRGCOBJ: TRadioGroup;
  gDlgRGMISC: TRadioGroup;

  // Optional COBJ recipe overrides (cobjrecipes.csv)
  // Stored as encoded strings: OutCount|WorkbenchKywd|CategoryKywd|CompPairsCSV
  // NOTE: CategoryKywd (FNAM) is reserved/legacy and is NOT read from cobjrecipes.csv.
  gCobjRecipeByMask: array[0..15] of TStringList; // key -> enc
  gCobjRecipeDefault: string;

  gCobjRecipesLoaded: boolean;
  gCobjRecipesHasDefaultRow: boolean;
  // Standalone COBJ-only (physical item) label file caches (testcobjlabeller.csv style)
  CobjWBToken: array[0..99] of string;              // WorkbenchName Text (token for EDID)
  CobjWBKywdEdid: array[0..99] of string;           // WorkbenchName Text treated as BNAM KYWD EditorID (best effort)
  CobjCatToken: array[0..9999] of string;           // CatName Text (token for EDID), key=wb*100+cat
  CobjCatKywdEdid: array[0..9999] of string;        // CatName Text treated as FNAM KYWD EditorID (best effort)
  CobjCatDefaultCreatedEdid: array[0..9999] of string;
  CobjCatDefaultNumCreate: array[0..9999] of integer;
  CobjCatDefaultCompsPairsCSV: array[0..9999] of string;
  
  // Build input dialog state (manual-first, CSV optional)
  gBuildInputUseCSV: boolean;
  gBuildInputForm: TForm;
  gBuildInputPairsRG: TRadioGroup;
  gBuildInputBaseEdit: TEdit;
  gBuildInputDestEdit: TEdit;
  gBuildInputBaseBtn: TButton;
  gBuildInputDestBtn: TButton;
 
   // last chosen CSV filenames (for debugging)
  gLastCSVDialogFilename: string;
  gLastBuildCSVPath: string;
  gLastLabelCSVPath: string;
  gLastCobjRecipeCSVPath: string;


  // Preloaded label CSV (used to detect intent and avoid redundant dialogs)
  gPreloadedLabelLines: TStringList;
  gPreloadedLabelKind: integer;

// label.csv maps
  ObjTypeTokenMap: array[0..99] of string;
  ObjNameTokenMap: array[0..99] of string;
  ObjNameTokenScoped: array[0..9999] of string;
  ObjNameTemplateEdidMap: array[0..99] of string;
  ObjNameTemplateEdidScoped: array[0..9999] of string;
  SlotLabelMap: array[0..99] of string;
  OptLabelMap: array[0..99] of string;

  // [LBL] label.csv optional BGSM pair overrides
  // Format on any label.csv row:
  //   Type,Index,Text,Base1,Dest1,Base2,Dest2,...
  // Pair resolution precedence when build.csv provides no explicit pairs (Base1 empty):
  //   MSWP (TTNNSSOO) > OPT (OO) > SLOT (SS) > OBJNAME (TTNN or NN) > OBJTYPE (TT) > built-in default path
  gPairsObjType: TStringList;
  gPairsObjName: TStringList;
  gPairsSlot: TStringList;
  gPairsOpt: TStringList;
  gPairsMSWP: TStringList;
  
  gSIGS: array[0..20] of string;
  gSIGSInit: boolean;

//============================================================================
//[UTIL] Helpers: strings, parsing, formatting
//============================================================================

function TrimOrEmpty(s: string): string;
begin
  Result := Trim(s);
end;

function StartsWithI(const s, prefix: string): boolean;
begin
  Result := Copy(LowerCase(s), 1, Length(prefix)) = LowerCase(prefix);
end;

function SafeInt(s: string; defaultValue: integer): integer;
var
  t: string;
begin
  t := Trim(s);
  if t = '' then begin
    Result := defaultValue;
    Exit;
  end;
  try
    Result := StrToInt(t);
  except
    Result := defaultValue;
  end;
end;

function SafeFloat(s: string; defaultValue: double): double;
var
  t: string;
begin
  t := Trim(s);
  if t = '' then begin
    Result := defaultValue;
    Exit;
  end;

  try
    Result := StrToFloat(t);
    Exit;
  except
  end;

  try
    Result := StrToFloat(StringReplace(t, ',', '.', [rfReplaceAll]));
    Exit;
  except
  end;

  try
    Result := StrToFloat(StringReplace(t, '.', ',', [rfReplaceAll]));
    Exit;
  except
    Result := defaultValue;
  end;
end;

function FloatToStrDot(v: double): string;
var
  s: string;
begin
  s := FormatFloat('0.############', v);
  s := StringReplace(s, ',', '.', [rfReplaceAll]);
  Result := s;
end;

function Pad2(value: integer): string;
var
  s: string;
begin
  s := IntToStr(value);
  while Length(s) < 2 do s := '0' + s;
  Result := s;
end;

function OptSuffix(hasOptDim: boolean; const optStr: string): string;
begin
  // If NumOpt = 0 (no opt dimension), omit any _optNN suffix entirely.
  if not hasOptDim then
    Result := ''
  else
    Result := '_opt' + optStr;
end;

function FindAnyRecordByEditorIDInFiles(const edid: string): IInterface;
var
  i: integer;
  rec: IInterface;
begin
  Result := nil;
  if Trim(edid) = '' then Exit;

  InitSIGS;

  for i := 0 to 20 do begin
    rec := FindRecordByEditorIDInFiles(gSIGS[i], edid);
    if Assigned(rec) then begin
      Result := rec;
      Exit;
    end;
  end;
end;

function CanonicalObjSeg(objTypeIdx, objNameIdx, numObjType: integer): string;
begin
  // For multi-ObjType projects, ObjName keys are 4 digits (ObjTypeNN + ObjNameNN),
  // e.g. 0000 vs 0100. Canonical segments must match to avoid EDID collisions.
  if numObjType > 1 then
    Result := 'obj' + Pad2(objTypeIdx) + Pad2(objNameIdx)
  else
    Result := 'obj' + Pad2(objNameIdx);
end;

function ObjTypeTokenForEDID(const rawToken: string): string;
var
  t: string;
begin
  t := LowerCase(Trim(rawToken));

  if (t = 'weapons') or (t = 'weapon') or (t = 'wpn') or (t = 'weap') then begin
    Result := 'weapon';
    Exit;
  end;

  if (t = 'clothes') or (t = 'clothing') or (t = 'cloths') or
     (t = 'armors') or (t = 'armor') or (t = 'armour') or
     (t = 'armours') or (t = 'armo') then begin
    Result := 'armor';
    Exit;
  end;

  if (t = 'npc') or (t = 'non-player character') or (t = 'nonplayercharacter') or
     (t = 'non_player_character') or (t = 'actors') or (t = 'actor') then begin
    Result := 'non-player character';
    Exit;
  end;

  Result := Trim(rawToken);
end;

function FreeTextTokenForEDID(const raw: string): string;
var
  s: string;
  i: integer;
  ch: string;
begin
  s := TrimOrEmpty(raw);
  s := LowerCase(s);
  Result := '';
  for i := 1 to Length(s) do begin
    ch := Copy(s, i, 1);
    if (ch >= 'a') and (ch <= 'z') then
      Result := Result + ch
    else if (ch >= '0') and (ch <= '9') then
      Result := Result + ch;
    // ignore everything else (spaces, underscores, dashes, punctuation)
  end;
end;

function NormalizeWorkbenchKywdEdid(const rawText: string): string;
var
  t: string;
begin
  // Standardizes free-form user input to canonical Workbench KYWD EditorIDs used by COBJ.BNAM.
  // Accepts variants like: chemlab, chemistry lab, chem_lab, lab, chem -> workbenchchemlab
  //                       cooking, cook, food, cook_pot, stove, oven  -> workbenchcooking
  t := LowerCase(Trim(rawText));
  // Normalize separators
  t := StringReplace(t, ' ',  '', [rfReplaceAll]);
  t := StringReplace(t, '_',  '', [rfReplaceAll]);
  t := StringReplace(t, '-',  '', [rfReplaceAll]);
  t := StringReplace(t, '.',  '', [rfReplaceAll]);

  // Chemlab
  if (t = 'workbenchchemlab') or (t = 'chemlab') or (t = 'chemistry') or (t = 'chemistrylab') or
     (t = 'chemistrylaboratory') or (t = 'chemlaboratory') or (t = 'chem') or (t = 'lab') then begin
    Result := 'workbenchchemlab';
    Exit;
  end;

  // Cooking
  if (t = 'workbenchcooking') or (t = 'cooking') or (t = 'cook') or (t = 'food') or
     (t = 'cookpot') or (t = 'stove') or (t = 'oven') then begin
    Result := 'workbenchcooking';
    Exit;
  end;

  // Fallback: keep as trimmed lowercase (still allows advanced users to supply exact KYWD EditorIDs)
  Result := LowerCase(Trim(rawText));
end;

function NormalizeCatNameToRecipeKywdEdid(const rawText: string): string;
var
  t: string;
begin
  // Standardizes CATNAME text to recipe-prefixed KYWD EditorIDs used by COBJ.FNAM.
  // Examples: utility -> recipeutility, RecipeDrug -> recipedrug
  t := LowerCase(Trim(rawText));
  t := StringReplace(t, ' ',  '', [rfReplaceAll]);
  t := StringReplace(t, '_',  '', [rfReplaceAll]);
  t := StringReplace(t, '-',  '', [rfReplaceAll]);
  if t = '' then begin
    Result := '';
    Exit;
  end;
  if Pos('recipe', t) = 1 then
    Result := t
  else
    Result := 'recipe' + t;
end;


function NormalizeSlotLabelForAP(const sIn: string): string;
var
  s: string;
begin
  // Strip trailing spaces
  s := sIn;
  while (Length(s) > 0) and (s[Length(s)] = ' ') do
    Delete(s, Length(s), 1);

  // If ends with '-', strip it and any trailing spaces
  if (Length(s) > 0) and (s[Length(s)] = '-') then begin
    Delete(s, Length(s), 1);
    while (Length(s) > 0) and (s[Length(s)] = ' ') do
      Delete(s, Length(s), 1);
  end;

  Result := s;
end;

function GetMapVal(const mapType: string; idx: integer): string;
var
  t: string;
begin
  Result := '';
  if (idx < 0) or (idx > 99) then Exit;

  t := UpperCase(mapType);
  if t = 'OBJTYPE' then Result := ObjTypeTokenMap[idx]
  else if t = 'OBJNAME' then Result := ObjNameTokenMap[idx]
  else if t = 'SLOT' then Result := SlotLabelMap[idx]
  else if t = 'OPT' then Result := OptLabelMap[idx];
end;

function ObjNameScopedKey(objTypeIdx, objNameIdx: integer): integer;
begin
  Result := (objTypeIdx * 100) + objNameIdx;
end;

function GetObjNameToken(objTypeIdx, objNameIdx, numObjType: integer): string;
var
  k: integer;
begin
  Result := '';
  if (objTypeIdx < 0) or (objTypeIdx > 99) then Exit;
  if (objNameIdx < 0) or (objNameIdx > 99) then Exit;

  k := ObjNameScopedKey(objTypeIdx, objNameIdx);

  // If the row expands over multiple ObjType indices, ObjName MUST be scoped (TTNN).
  if numObjType > 1 then begin
    Result := ObjNameTokenScoped[k];
    Exit;
  end;

  // Single-ObjType rows: allow legacy 2-digit ObjName mapping, but prefer scoped if provided.
  if Trim(ObjNameTokenScoped[k]) <> '' then
    Result := ObjNameTokenScoped[k]
  else
    Result := ObjNameTokenMap[objNameIdx];
end;

function GetObjNameTemplateEdid(objTypeIdx, objNameIdx, numObjType: integer): string;
var
  k: integer;
begin
  Result := '';
  if (objTypeIdx < 0) or (objTypeIdx > 99) then Exit;
  if (objNameIdx < 0) or (objNameIdx > 99) then Exit;

  k := ObjNameScopedKey(objTypeIdx, objNameIdx);

  // If the row expands over multiple ObjType indices, ObjName MUST be scoped (TTNN).
  if numObjType > 1 then begin
    Result := ObjNameTemplateEdidScoped[k];
  end else begin
    // Prefer unscoped template if provided; else allow scoped.
    if Trim(ObjNameTemplateEdidMap[objNameIdx]) <> '' then
      Result := ObjNameTemplateEdidMap[objNameIdx]
    else
      Result := ObjNameTemplateEdidScoped[k];
  end;
end;

procedure ClearLabelMaps;
var
  i: integer;
begin
  for i := 0 to 99 do begin
    ObjTypeTokenMap[i] := '';
    ObjNameTokenMap[i] := '';
    ObjNameTemplateEdidMap[i] := '';
    SlotLabelMap[i] := '';
    OptLabelMap[i] := '';
  end;

  for i := 0 to 9999 do begin
    ObjNameTokenScoped[i] := '';
    ObjNameTemplateEdidScoped[i] := '';
  end;

  // optional pair overrides
  if Assigned(gPairsObjType) then gPairsObjType.Clear;
  if Assigned(gPairsObjName) then gPairsObjName.Clear;
  if Assigned(gPairsSlot) then gPairsSlot.Clear;
  if Assigned(gPairsOpt) then gPairsOpt.Clear;
  if Assigned(gPairsMSWP) then gPairsMSWP.Clear;
end;

procedure InitSIGS;
begin
  if gSIGSInit then Exit;
  gSIGS[0] := 'ALCH';
  gSIGS[1] := 'MISC';
  gSIGS[2] := 'ARMO';
  gSIGS[3] := 'WEAP';
  gSIGS[4] := 'AMMO';
  gSIGS[5] := 'STAT';
  gSIGS[6] := 'BOOK';
  gSIGS[7] := 'NOTE';
  gSIGS[8] := 'KEYM';
  gSIGS[9] := 'OMOD';
  gSIGS[10] := 'DOOR';
  gSIGS[11] := 'FURN';
  gSIGS[12] := 'ACTI';
  gSIGS[13] := 'FLST';
  gSIGS[14] := 'CONT';
  gSIGS[15] := 'FLOR';
  gSIGS[16] := 'LIGH';
  gSIGS[17] := 'MSTT';
  gSIGS[18] := 'SCOL';
  gSIGS[19] := 'TERM';
  gSIGS[20] := 'NPC_';
  
  gSIGSInit := True;
end;

// ============================================================================
// [CSV] CSV loaders
// ============================================================================
function LoadCSVWithDialog(slOut: TStringList; dialogTitle: string; allowHeaderSkip: boolean): boolean;
var
  OpenDialog: TOpenDialog;
  tmp: TStringList;
  firstLine: string;
  slTest: TStringList;
begin
  Result := False;
  if not Assigned(slOut) then Exit;

  OpenDialog := TOpenDialog.Create(nil);
  tmp := TStringList.Create;
  slTest := TStringList.Create;
  try
    OpenDialog.Title := dialogTitle;
    OpenDialog.Filter := 'CSV files (*.csv)|*.csv';
    OpenDialog.DefaultExt := 'csv';
    OpenDialog.InitialDir := ProgramPath;

    if not OpenDialog.Execute then Exit;

    // remember what the user actually selected
    gLastCSVDialogFilename := OpenDialog.FileName;

    tmp.LoadFromFile(OpenDialog.FileName);
    slOut.Clear;

    if (tmp.Count > 0) and allowHeaderSkip then begin
      firstLine := Trim(tmp[0]);
      slTest.StrictDelimiter := True;
      slTest.Delimiter := ',';
      slTest.DelimitedText := firstLine;
      // naive header detection: first column "Type" or "ProjectID"
      if (slTest.Count > 0) and ((UpperCase(Trim(slTest[0])) = 'TYPE') or (UpperCase(Trim(slTest[0])) = 'PROJECTID')) then
        tmp.Delete(0);
    end;

    slOut.Assign(tmp);
    Result := True;
  finally
    slTest.Free;
    tmp.Free;
    OpenDialog.Free;
  end;
end;


// @anchor LBL_ExtractPairsCSVFromTokens
function ExtractPairsCSVFromTokens(sl: TStringList; startIdx: integer): string;
var
  i: integer;
  baseVal, destVal: string;
  first: boolean;
begin
  // Returns a compact "Base1,Dest1,Base2,Dest2,..." CSV string.
  // If no Base* is provided (all empty), returns ''.
  Result := '';
  if not Assigned(sl) then Exit;
  if (startIdx < 0) or (startIdx >= sl.Count) then Exit;

  first := True;
  i := startIdx;
  while i < sl.Count do begin
    baseVal := TrimOrEmpty(sl[i]);
    if baseVal = '' then Break;

    destVal := '';
    if (i + 1) < sl.Count then
      destVal := TrimOrEmpty(sl[i + 1]);
    if destVal = '' then destVal := baseVal;

    if not first then Result := Result + ',';
    Result := Result + baseVal + ',' + destVal;
    first := False;

    i := i + 2;
  end;
end;

// @anchor LBL_PairsCSVHasData
function PairsCSVHasData(pairsCSV: string): boolean;
var
  tmp: TStringList;
begin
  Result := False;
  if Trim(pairsCSV) = '' then Exit;

  tmp := TStringList.Create;
  try
    tmp.StrictDelimiter := True;
    tmp.Delimiter := ',';
    tmp.DelimitedText := pairsCSV;

    // Require a non-empty first base token.
    if (tmp.Count > 0) and (TrimOrEmpty(tmp[0]) <> '') then
      Result := True;
  finally
    tmp.Free;
  end;
end;



function NormalizeMSWPKey(idxStr: string; var objTypeIdxOut: integer; var objNameIdxOut: integer; var slotIdxOut: integer; var optIdxOut: integer): string;
var
  t: string;
begin
  Result := '';
  objTypeIdxOut := -1;
  objNameIdxOut := -1;
  slotIdxOut := -1;
  optIdxOut := -1;

  // Expected: TTNNSSOO
  t := Trim(idxStr);
  if Length(t) <> 8 then Exit;

  objTypeIdxOut := StrToIntDef(Copy(t, 1, 2), -1);
  objNameIdxOut := StrToIntDef(Copy(t, 3, 2), -1);
  slotIdxOut    := StrToIntDef(Copy(t, 5, 2), -1);
  optIdxOut     := StrToIntDef(Copy(t, 7, 2), -1);

  if (objTypeIdxOut < 0) or (objTypeIdxOut > 99) then Exit;
  if (objNameIdxOut < 0) or (objNameIdxOut > 99) then Exit;
  if (slotIdxOut < 0) or (slotIdxOut > 99) then Exit;
  if (optIdxOut < 0) or (optIdxOut > 99) then Exit;

  Result := Pad2(objTypeIdxOut) + Pad2(objNameIdxOut) + Pad2(slotIdxOut) + Pad2(optIdxOut);
end;


// Detect whether the selected label CSV is:
//  - Standard "OMOD-style" label.csv (ObjType/ObjName/Slot/Opt/MSWP), which needs build input; or
//  - Standalone "COBJ-only physical item" label CSV (WorkbenchName/CatName/ItemName), which does NOT.
function DetectLabelCSVKind(slLines: TStringList): integer;
var
  sl: TStringList;
  i: integer;
  line, t: string;
begin
  // Default to OMOD-style to avoid accidentally suppressing required build UI.
  Result := LBL_KIND_OMOD_LABEL;
  if not Assigned(slLines) then Exit;

  sl := TStringList.Create;
  try
    sl.StrictDelimiter := True;
    sl.Delimiter := ',';

    for i := 0 to slLines.Count - 1 do begin
      line := Trim(slLines[i]);
      if line = '' then Continue;
      if Copy(line, 1, 1) = '#' then Continue;

      sl.Clear;
      sl.DelimitedText := line;
      if sl.Count < 1 then Continue;

      t := UpperCase(TrimOrEmpty(GetCol(sl, 0)));
      if t = '' then Continue;

      // Skip header-ish rows (some CSVs omit headers entirely).
      if (t = 'TYPE') or (t = 'T') then Continue;

      // Standalone COBJ-only label tokens
      if (t = 'WORKBENCHNAME') or (t = 'WORKBENCH') or (t = 'CATNAME') or (t = 'ITEMNAME') or
         (t = 'CREATEDITEM') or (t = 'NUMCREATE') then begin
        Result := LBL_KIND_COBJ_ONLY;
        Exit;
      end;

      // Standard OMOD/COBJ-for-OMOD label tokens
      if (t = 'OBJTYPE') or (t = 'OBJNAME') or (t = 'SLOT') or (t = 'OPT') or (t = 'MSWP') then begin
        Result := LBL_KIND_OMOD_LABEL;
        Exit;
      end;

      // Heuristic fallback (first non-header token wins).
      if StartsWithI(t, 'WORKBENCH') or StartsWithI(t, 'CAT') or StartsWithI(t, 'ITEM') then begin
        Result := LBL_KIND_COBJ_ONLY;
        Exit;
      end;

      Result := LBL_KIND_OMOD_LABEL;
      Exit;
    end;
  finally
    sl.Free;
  end;
end;

// @anchor LBL_LoadLabelCSVRequired
function LoadLabelCSVRequired: boolean;
var
  slLines: TStringList;
  sl: TStringList;
  i, idx, idx2: integer;
  line, mapType, idxStr, txt, templateEdid: string;
  pairsCSV, key: string;
  pairsStart: integer;
  hasTemplateEdidColumn: boolean;
  ot, obn, ss, oo: integer;
begin
  Result := False;

  ClearLabelMaps;

  slLines := TStringList.Create;
  sl := TStringList.Create;
  try
    if Assigned(gPreloadedLabelLines) and (gPreloadedLabelLines.Count > 0) then begin
      slLines.Assign(gPreloadedLabelLines);
      // gLastLabelCSVPath already set by caller when preloading
    end else begin
      if not LoadCSVWithDialog(slLines, 'Select label.csv', True) then Exit;
      gLastLabelCSVPath := gLastCSVDialogFilename;
    end;

    sl.StrictDelimiter := True;
    sl.Delimiter := ',';

    
  // Detect whether label.csv includes TemplateEdid as column 4 (Type,Index,Text,TemplateEdid,Base1,Dest1,...)
  // We infer this by looking for the first OBJNAME row whose 4th column looks like an EditorID (not a BGSM path).
  hasTemplateEdidColumn := False;
  for i := 0 to slLines.Count - 1 do begin
    line := Trim(slLines[i]);
    if line = '' then Continue;
    sl.CommaText := line;

    if sl.Count < 4 then Continue;
    mapType := UpperCase(Trim(sl[0]));
    if mapType <> 'OBJNAME' then Continue;
    if TrimOrEmpty(sl[3]) = '' then Continue;

    // Heuristic: template EditorIDs usually contain no slashes and do not end in .bgsm
    if (Pos('\', sl[3]) = 0) and (Pos('/', sl[3]) = 0) and (Pos('.bgsm', LowerCase(sl[3])) = 0) then begin
      hasTemplateEdidColumn := True;
      Break;
    end;
  end;

for i := 0 to slLines.Count - 1 do begin
      line := Trim(slLines[i]);
      if line = '' then Continue;

      sl.Clear;
      sl.DelimitedText := slLines[i];
      if sl.Count < 3 then Continue;

      mapType := UpperCase(Trim(sl[0]));
      idxStr  := Trim(sl[1]);
      txt     := sl[2]; // do NOT trim; slot labels may include trailing spaces.

      templateEdid := '';
    pairsStart := 3;
    if hasTemplateEdidColumn then begin
      if sl.Count > 3 then
        templateEdid := TrimOrEmpty(sl[3]);
      pairsStart := 4;
    end;
    pairsCSV := ExtractPairsCSVFromTokens(sl, pairsStart);

      // MSWP optional per-record override: Index must be TTNNSSOO (8 digits).
      // Text column can be blank; pairs must start at Base1.
      if mapType = 'MSWP' then begin
        key := NormalizeMSWPKey(idxStr, ot, obn, ss, oo);
        if (key <> '') and (PairsCSVHasData(pairsCSV)) and Assigned(gPairsMSWP) then
          gPairsMSWP.Values[key] := pairsCSV;
        Continue;
      end;

      if mapType = 'OBJNAME' then begin
        // ObjName supports either 2-digit NN (legacy) or 4-digit TTNN (scoped to ObjType).
        if Length(idxStr) = 4 then begin
          // TTNN
          idx := StrToIntDef(Copy(idxStr, 1, 2), -1);     // ObjTypeIdx (TT)
          idx2 := StrToIntDef(Copy(idxStr, 3, 2), -1);    // ObjNameIdx (NN)
          if (idx < 0) or (idx > 99) then Continue;
          if (idx2 < 0) or (idx2 > 99) then Continue;
          ObjNameTokenScoped[ObjNameScopedKey(idx, idx2)] := txt;
          if (hasTemplateEdidColumn) and (Trim(templateEdid) <> '') then
            ObjNameTemplateEdidScoped[ObjNameScopedKey(idx, idx2)] := templateEdid;

          if (PairsCSVHasData(pairsCSV)) and Assigned(gPairsObjName) then begin
            key := Pad2(idx) + Pad2(idx2);
            gPairsObjName.Values[key] := pairsCSV;
          end;
        end else begin
          // NN
          idx := StrToIntDef(idxStr, -1);
          if (idx < 0) or (idx > 99) then Continue;
          ObjNameTokenMap[idx] := txt;
          if (hasTemplateEdidColumn) and (Trim(templateEdid) <> '') then
            ObjNameTemplateEdidMap[idx] := templateEdid;

          if (PairsCSVHasData(pairsCSV)) and Assigned(gPairsObjName) then begin
            key := Pad2(idx);
            gPairsObjName.Values[key] := pairsCSV;
          end;
        end;
      end else begin
        idx := StrToIntDef(idxStr, -1);
        if (idx < 0) or (idx > 99) then Continue;

        if mapType = 'OBJTYPE' then begin
          ObjTypeTokenMap[idx] := txt;
          if (PairsCSVHasData(pairsCSV)) and Assigned(gPairsObjType) then
            gPairsObjType.Values[Pad2(idx)] := pairsCSV;
        end
        else if mapType = 'SLOT' then begin
          SlotLabelMap[idx] := txt;
          if (PairsCSVHasData(pairsCSV)) and Assigned(gPairsSlot) then
            gPairsSlot.Values[Pad2(idx)] := pairsCSV;
        end
        else if mapType = 'OPT' then begin
          OptLabelMap[idx] := txt;
          if (PairsCSVHasData(pairsCSV)) and Assigned(gPairsOpt) then
            gPairsOpt.Values[Pad2(idx)] := pairsCSV;
        end;
      end;
    end;

    
  // summary (helps catch wrong file selection instantly)
  ot := 0; obn := 0; ss := 0; oo := 0;
  for i := 0 to 99 do begin
    if Trim(ObjTypeTokenMap[i]) <> '' then Inc(ot);
    if Trim(ObjNameTokenMap[i]) <> '' then Inc(obn);
    if Trim(SlotLabelMap[i]) <> '' then Inc(ss);
    if Trim(OptLabelMap[i]) <> '' then Inc(oo);
  end;

  idx2 := 0; // reuse idx2 as scoped count accumulator
  for i := 0 to 9999 do
    if Trim(ObjNameTokenScoped[i]) <> '' then Inc(idx2);

  AddMessage('Loaded label.csv: ' + gLastLabelCSVPath + ' | ObjType=' + IntToStr(ot) +
             ' ObjName=' + IntToStr(obn) + ' ObjNameScoped=' + IntToStr(idx2) +
             ' Slot=' + IntToStr(ss) + ' Opt=' + IntToStr(oo));

  if ot = 0 then begin
    AddMessage('ERROR: label.csv loaded 0 ObjType entries. You likely selected the wrong file or the file is not in Type,Index,Text format.');
    Exit;
  end;

Result := True;
  finally
    sl.Free;
    slLines.Free;
  end;
end;

// ============================================================================
// [COBJ] cobjrecipes.csv optional recipe overrides
// ============================================================================
procedure InitCobjRecipeMaps;
var
  m: integer;
begin
  // Ensure all per-mask maps exist (TStringList used as key->value map via .Values[])
  for m := 0 to 15 do begin
    if not Assigned(gCobjRecipeByMask[m]) then begin
      gCobjRecipeByMask[m] := TStringList.Create;
      gCobjRecipeByMask[m].NameValueSeparator := '=';
    end;
  end;
end;

procedure ClearCobjRecipeMaps;
var
  m: integer;
begin
  InitCobjRecipeMaps;
  for m := 0 to 15 do
    gCobjRecipeByMask[m].Clear;
  gCobjRecipeDefault := '';
  gCobjRecipesLoaded := False;
  gCobjRecipesHasDefaultRow := False;
end;
procedure ClearStandaloneCobjCaches;
var
  i: integer;
begin
  for i := 0 to 99 do begin
    CobjWBToken[i] := '';
    CobjWBKywdEdid[i] := '';
  end;
  for i := 0 to 9999 do begin
    CobjCatToken[i] := '';
    CobjCatKywdEdid[i] := '';
    CobjCatDefaultCreatedEdid[i] := '';
    CobjCatDefaultNumCreate[i] := 0;
    CobjCatDefaultCompsPairsCSV[i] := '';
  end;
end;

function CobjCatKey(wbIdx, catIdx: integer): integer;
begin
  Result := (wbIdx * 100) + catIdx;
end;

function BuildCompsPairsCSVFromRow(sl: TStringList; startCol: integer): string;
var
  col: integer;
  compName, compCount: string;
begin
  Result := '';
  if not Assigned(sl) then Exit;
  col := startCol;
  while col < sl.Count do begin
    compName := TrimOrEmpty(GetCol(sl, col));
    compCount := TrimOrEmpty(GetCol(sl, col + 1));
    col := col + 2;
    if compName = '' then Continue;
    if LowerCase(compName) = 'null' then begin
      Result := 'null';
      Exit;
    end;
    if compCount = '' then compCount := '1';
    if Result <> '' then Result := Result + ',';
    Result := Result + compName + ',' + compCount;
  end;
end;

function LoadStandaloneCobjLabelCSVAndBuild: boolean;
var
  slLines, sl: TStringList;
  i: integer;
  line, t, idxStr, txt: string;
  createdEdid: string;
  numCreateStr: string;
  compsPairsCSV: string;
  wbIdx, catIdx, itemIdx: integer;
  idxInt, remInt: integer;
  catKey: integer;
  cobjEdid: string;
  wbTok, catTok, itemTok: string;
  bnamKywdEdid, fnamKywdEdid: string;
  effCreatedEdid: string;
  effNumCreate: integer;
  effCompsPairsCSV: string;
  createdRec: IInterface;
  recipeEnc: string;
begin
  Result := False;

  ClearStandaloneCobjCaches;

  slLines := TStringList.Create;
  sl := TStringList.Create;
  try
    if Assigned(gPreloadedLabelLines) and (gPreloadedLabelLines.Count > 0) then begin
      slLines.Assign(gPreloadedLabelLines);
    end else begin
      if not LoadCSVWithDialog(slLines, 'Select COBJ-only label CSV', True) then Exit;
    end;

    sl.StrictDelimiter := True;
    sl.Delimiter := ',';

    // Pass 1: load WorkbenchName + CatName defaults
    for i := 0 to slLines.Count - 1 do begin
      line := Trim(slLines[i]);
      if line = '' then Continue;
      sl.Clear;
      sl.DelimitedText := slLines[i];
      if sl.Count < 3 then Continue;

      t := UpperCase(Trim(sl[0]));
      idxStr := TrimOrEmpty(GetCol(sl, 1));
      txt := TrimOrEmpty(GetCol(sl, 2));

      // Columns expected (extra columns optional):
      // Type,Index,Text,CreatedItem,NumCreate,CompName1,NumComp1,CompName2,NumComp2,...
      createdEdid := TrimOrEmpty(GetCol(sl, 3));
      numCreateStr := TrimOrEmpty(GetCol(sl, 4));
      compsPairsCSV := BuildCompsPairsCSVFromRow(sl, 5);

      if (t = 'WORKBENCHNAME') or (t = 'WORKBENCH') then begin
        txt := NormalizeWorkbenchKywdEdid(txt);
        wbIdx := SafeInt(idxStr, -1);
        if (wbIdx < 0) or (wbIdx > 99) then Continue;
        CobjWBToken[wbIdx] := txt;
        CobjWBKywdEdid[wbIdx] := txt; // best-effort: treat Text as KYWD EditorID for BNAM
      end else if (t = 'CATNAME') then begin
        // Index formats accepted:
        //  - 4-digit WWCC (preferred): wb=00..99, cat=00..99
        //  - numeric wb*100+cat (fallback): 0..9999 (leading zeros omitted)
        wbIdx := -1;
        catIdx := -1;
        if Length(idxStr) = 4 then begin
          wbIdx := StrToIntDef(Copy(idxStr, 1, 2), -1);
          catIdx := StrToIntDef(Copy(idxStr, 3, 2), -1);
        end else begin
          idxInt := SafeInt(idxStr, -1);
          if idxInt >= 0 then begin
            wbIdx := idxInt div 100;
            catIdx := idxInt mod 100;
          end;
        end;
        if (wbIdx < 0) or (wbIdx > 99) or (catIdx < 0) or (catIdx > 99) then begin
          AddMessage('WARNING: Skipping CatName with invalid Index: ' + idxStr);
          Continue;
        end;
        catKey := CobjCatKey(wbIdx, catIdx);
        txt := NormalizeCatNameToRecipeKywdEdid(txt);
        CobjCatToken[catKey] := txt;
        CobjCatKywdEdid[catKey] := txt; // best-effort: treat Text as KYWD EditorID for FNAM
        if createdEdid <> '' then CobjCatDefaultCreatedEdid[catKey] := createdEdid;
        if numCreateStr <> '' then CobjCatDefaultNumCreate[catKey] := SafeInt(numCreateStr, 0);
        if compsPairsCSV <> '' then CobjCatDefaultCompsPairsCSV[catKey] := compsPairsCSV;
      end;
    end;

    // Pass 2: build ItemName COBJs
    for i := 0 to slLines.Count - 1 do begin
      line := Trim(slLines[i]);
      if line = '' then Continue;
      sl.Clear;
      sl.DelimitedText := slLines[i];
      if sl.Count < 3 then Continue;

      t := UpperCase(Trim(sl[0]));
      if t <> 'ITEMNAME' then Continue;

      idxStr := TrimOrEmpty(GetCol(sl, 1));
      itemTok := TrimOrEmpty(GetCol(sl, 2));

      createdEdid := TrimOrEmpty(GetCol(sl, 3));
      numCreateStr := TrimOrEmpty(GetCol(sl, 4));
      compsPairsCSV := BuildCompsPairsCSVFromRow(sl, 5);

            // Index formats accepted:
      //  - 6-digit WWCCII (preferred): wb=00..99, cat=00..99, item=00..99
      //  - numeric wb*10000 + cat*100 + item (fallback): 0..999999 (leading zeros omitted)
      wbIdx := -1;
      catIdx := -1;
      itemIdx := -1;
      if Length(idxStr) = 6 then begin
        wbIdx := StrToIntDef(Copy(idxStr, 1, 2), -1);
        catIdx := StrToIntDef(Copy(idxStr, 3, 2), -1);
        itemIdx := StrToIntDef(Copy(idxStr, 5, 2), -1);
      end else begin
        idxInt := SafeInt(idxStr, -1);
        if idxInt >= 0 then begin
          wbIdx := idxInt div 10000;
          remInt := idxInt mod 10000;
          catIdx := remInt div 100;
          itemIdx := remInt mod 100;
        end;
      end;
      if (wbIdx < 0) or (wbIdx > 99) or (catIdx < 0) or (catIdx > 99) or (itemIdx < 0) or (itemIdx > 99) then begin
        AddMessage('WARNING: Skipping ItemName with invalid Index: ' + idxStr);
        Continue;
      end;
      catKey := CobjCatKey(wbIdx, catIdx);

      wbTok := CobjWBToken[wbIdx];
      catTok := CobjCatToken[catKey];
      if wbTok = '' then wbTok := 'wb' + Pad2(wbIdx);
      if catTok = '' then catTok := 'cat' + Pad2(catIdx);
      if itemTok = '' then itemTok := 'item' + Pad2(itemIdx);

      // Resolve BNAM/FNAM keyword EditorIDs (best-effort)
      bnamKywdEdid := CobjWBKywdEdid[wbIdx];
      fnamKywdEdid := CobjCatKywdEdid[catKey];

      // Inheritance: Item overrides Category defaults
      effCreatedEdid := createdEdid;
      if effCreatedEdid = '' then effCreatedEdid := CobjCatDefaultCreatedEdid[catKey];

      effNumCreate := SafeInt(numCreateStr, 0);
      if effNumCreate <= 0 then effNumCreate := CobjCatDefaultNumCreate[catKey];
      if effNumCreate <= 0 then effNumCreate := 1;

      effCompsPairsCSV := compsPairsCSV;
      if effCompsPairsCSV = '' then effCompsPairsCSV := CobjCatDefaultCompsPairsCSV[catKey];

      // COBJ-only physical item recipes cannot be free in-game; enforce at least one component.
      if (Trim(effCompsPairsCSV) = '') or (LowerCase(Trim(effCompsPairsCSV)) = 'null') then
        effCompsPairsCSV := 'c_steel,1';

      if effCreatedEdid = '' then begin
        AddMessage('WARNING: Skipping standalone COBJ (missing CreatedItem): ItemName ' + idxStr);
        Continue;
      end;

      createdRec := FindAnyRecordByEditorIDInFiles(effCreatedEdid);
      if not Assigned(createdRec) then begin
        AddMessage('WARNING: Could not resolve CreatedItem "' + effCreatedEdid + '" for ItemName ' + idxStr);
        Continue;
      end;

      // Deterministic EDID
      cobjEdid := 'co_' + wbTok + '_' + catTok + '_' + itemTok;

      recipeEnc := EncodeCobjRecipe(IntToStr(effNumCreate), bnamKywdEdid, fnamKywdEdid, effCompsPairsCSV);
      EnsureAndRebuildCOBJ(gTargetFile, cobjEdid, createdRec, recipeEnc);
    end;

    Result := True;
  finally
    sl.Free;
    slLines.Free;
  end;
end;

function EncodeCobjRecipe(outCountStr: string; wbKywdEdid: string; catKywdEdid: string; compsPairsCSV: string): string;
begin
  // Encoding: OutCount|WorkbenchKywd|CategoryKywd|Comp1,Count1,Comp2,Count2,...
  Result := Trim(outCountStr) + '|' + Trim(wbKywdEdid) + '|' + Trim(catKywdEdid) + '|' + Trim(compsPairsCSV);
end;

function CobjRecipeKeyForMask(mask: integer; objTypeIdx, objNameIdx, slotIdx, optIdx: integer): string;
begin
  Result := '';
  if (mask and 8) <> 0 then Result := Result + Pad2(objTypeIdx);
  if (mask and 4) <> 0 then Result := Result + Pad2(objNameIdx);
  if (mask and 2) <> 0 then Result := Result + Pad2(slotIdx);
  if (mask and 1) <> 0 then Result := Result + Pad2(optIdx);
end;

procedure CobjRecipe_ParseEnc(const enc: string; var aOut, aWb, aCat, aComps: string);
var
  tmp: TStringList;
begin
  aOut := '';
  aWb := '';
  aCat := '';
  aComps := '';
  if Trim(enc) = '' then Exit;

  tmp := TStringList.Create;
  try
    tmp.StrictDelimiter := True;
    tmp.Delimiter := '|';
    tmp.DelimitedText := enc;
    if tmp.Count > 0 then aOut := TrimOrEmpty(tmp[0]);
    if tmp.Count > 1 then aWb  := TrimOrEmpty(tmp[1]);
    if tmp.Count > 2 then aCat := TrimOrEmpty(tmp[2]);
    if tmp.Count > 3 then aComps := TrimOrEmpty(tmp[3]);
  finally
    tmp.Free;
  end;
end;

procedure CobjRecipe_ApplyOverlay(const enc: string; var outStr, wbStr, catStr, compsStr: string);
var
  tOut, tWb, tCat, tComps: string;
begin
  CobjRecipe_ParseEnc(enc, tOut, tWb, tCat, tComps);

  // OutCount: blank means "no override"
  if Trim(tOut) <> '' then outStr := tOut;

  // BNAM/FNAM overrides: blank means "no override"
  if Trim(tWb)  <> '' then wbStr := tWb;
  if Trim(tCat) <> '' then catStr := tCat;

  // Components: blank means "no override"; "null" can be used to explicitly clear.
  if Trim(tComps) <> '' then compsStr := tComps;
end;

function ResolveCobjRecipe(objTypeIdx, objNameIdx, slotIdx, optIdx: integer): string;
var
  outStr, wbStr, catStr, compsStr: string;
  i, mask: integer;
  k, v: string;
begin
  Result := '';

  if not gCobjRecipesLoaded then
    Exit; // No cobjrecipes.csv selected/loaded; do not override built-in recipe behavior.

  // Start with default row (mask=0), if present.
  outStr := '';
  wbStr := '';
  catStr := '';
  compsStr := '';
  if Trim(gCobjRecipeDefault) <> '' then
    CobjRecipe_ApplyOverlay(gCobjRecipeDefault, outStr, wbStr, catStr, compsStr);

  // Apply overlays from least->most specific.
  for i := 0 to 14 do begin
    // Precedence (least->most):
    //  OT, ON, S, O, OT+ON, OT+S, OT+O, ON+S, ON+O, S+O, OT+S+O, OT+ON+S, OT+ON+O, ON+S+O, Exact
    case i of
      0:  mask := 8;   // OT
      1:  mask := 4;   // ON
      2:  mask := 2;   // S
      3:  mask := 1;   // O
      4:  mask := 12;  // OT+ON
      5:  mask := 10;  // OT+S
      6:  mask := 9;   // OT+O
      7:  mask := 6;   // ON+S
      8:  mask := 5;   // ON+O
      9:  mask := 3;   // S+O
      10: mask := 11;  // OT+S+O
      11: mask := 14;  // OT+ON+S
      12: mask := 13;  // OT+ON+O
      13: mask := 7;   // ON+S+O
      14: mask := 15;  // OT+ON+S+O (Exact)
    else
      mask := 0;
    end;

    if mask = 0 then Continue;

    // Skip masks requiring fields we don't have (caller can pass -1 for "not applicable").
    if ((mask and 8) <> 0) and (objTypeIdx < 0) then Continue;
    if ((mask and 4) <> 0) and (objNameIdx < 0) then Continue;
    if ((mask and 2) <> 0) and (slotIdx    < 0) then Continue;
    if ((mask and 1) <> 0) and (optIdx     < 0) then Continue;

    k := CobjRecipeKeyForMask(mask, objTypeIdx, objNameIdx, slotIdx, optIdx);
    if Assigned(gCobjRecipeByMask[mask]) then begin
      v := gCobjRecipeByMask[mask].Values[k];
      if Trim(v) <> '' then
        CobjRecipe_ApplyOverlay(v, outStr, wbStr, catStr, compsStr);
    end;
  end;

  // If nothing matched (and no default), return blank so EnsureAndRebuildCOBJ uses its built-in defaults.
  // However, if the user provided cobjrecipes.csv WITHOUT a default row, then "no components specified anywhere" means FREE.
  if (Trim(outStr) = '') and (Trim(wbStr) = '') and (Trim(catStr) = '') and (Trim(compsStr) = '') then begin
    if not gCobjRecipesHasDefaultRow then
      Result := EncodeCobjRecipe('', '', '', 'null')
    else
      Result := '';
    Exit;
  end;

  // If a recipe was found/assembled but components never resolved, treat as FREE only when no default row exists.
  if (Trim(compsStr) = '') and (not gCobjRecipesHasDefaultRow) then
    compsStr := 'null';

  Result := EncodeCobjRecipe(outStr, wbStr, catStr, compsStr);
end;

function CobjRecipe_IsValidSpecifiedIdx(const raw: string; idx: integer): boolean;
begin
  // Blank is wildcard (valid). Non-blank must be 0..99.
  if Trim(raw) = '' then begin
    Result := True;
    Exit;
  end;
  Result := (idx >= 0) and (idx <= 99);
end;

function LoadCobjRecipesCSVOptional: boolean;
var
  slLines, sl: TStringList;
  i, col, compsStart: integer;
  line: string;

  objTypeStr, objNameStr, slotStr, optStr: string;
  outCountStr: string;

  compName, compCount: string;
  compsPairsCSV: string;

  enc, key: string;
  objTypeIdx, objNameIdx, slotIdx, optIdx: integer;
  mask: integer;

  c5, c6, c7: string;
  n6, n7: integer;
begin
  Result := False;

  // This optional CSV is not loaded until the user selects it successfully.
  gCobjRecipesLoaded := False;
  gCobjRecipesHasDefaultRow := False;

  ClearCobjRecipeMaps;

  slLines := TStringList.Create;
  sl := TStringList.Create;
  try
    if not LoadCSVWithDialog(slLines, 'Optional: Select cobjrecipes.csv (Cancel to skip)', True) then begin
      Result := False;
      Exit;
    end;

    sl.StrictDelimiter := True;
    sl.Delimiter := ',';

    for i := 0 to slLines.Count - 1 do begin
      line := Trim(slLines[i]);
      if line = '' then Continue;

      sl.Clear;
      sl.DelimitedText := line;

      // Need at least: ObjType, ObjName, Slot, Opt, OutputCount
      if sl.Count < 5 then Continue;

      // Columns (NEW format):
      //  0 ObjType##   (blank = wildcard)
      //  1 ObjName##   (blank = wildcard)
      //  2 Slot##      (blank = wildcard)
      //  3 Opt##       (blank = wildcard)
      //  4 Output Count
      //  5+ Component Name N, Component Count N ...
      //
      // Legacy support: if the CSV still contains an extra column 5 for CategoryKywd (FNAM),
      // we ignore it and treat components as starting at column 6 instead.
      objTypeStr := TrimOrEmpty(GetCol(sl, 0));
      objNameStr := TrimOrEmpty(GetCol(sl, 1));
      slotStr    := TrimOrEmpty(GetCol(sl, 2));
      optStr     := TrimOrEmpty(GetCol(sl, 3));

      outCountStr := TrimOrEmpty(GetCol(sl, 4));

      // Determine component start column (5 = new format, 6 = legacy format w/ CategoryKywd in col 5).
      compsStart := 5;
      if sl.Count >= 6 then begin
        c5 := TrimOrEmpty(GetCol(sl, 5));

        // If we have enough columns to inspect the next two fields, detect legacy by pattern:
        // legacy: col6 is compName (non-numeric), col7 is compCount (numeric)
        // new:    col6 is usually compCount (numeric) for compName in col5
        if sl.Count >= 8 then begin
          c6 := TrimOrEmpty(GetCol(sl, 6));
          c7 := TrimOrEmpty(GetCol(sl, 7));
          n6 := SafeInt(c6, -9999);
          n7 := SafeInt(c7, -9999);

          if (Trim(c6) <> '') and (n6 = -9999) and (n7 <> -9999) then
            compsStart := 6;
        end else begin
          // No room to inspect (no components). If col5 looks like a recipe category, treat as legacy.
          if (c5 <> '') and (LowerCase(Copy(c5, 1, 5)) = 'recipe') then
            compsStart := 6;
        end;
      end;

      // Components (pairs)
      compsPairsCSV := '';
      col := compsStart;
      while col < sl.Count do begin
        compName := TrimOrEmpty(GetCol(sl, col));
        if compName = '' then begin
          col := col + 2;
          Continue;
        end;

        // Special case: "null" means explicitly no components
        if LowerCase(compName) = 'null' then begin
          compsPairsCSV := 'null';
          Break;
        end;

        if (col + 1) >= sl.Count then Break; // dangling comp name w/o count
        compCount := TrimOrEmpty(GetCol(sl, col + 1));
        if compCount = '' then compCount := '1';

        if compsPairsCSV <> '' then compsPairsCSV := compsPairsCSV + ',';
        compsPairsCSV := compsPairsCSV + compName + ',' + compCount;

        col := col + 2;
      end;

      // No workbench keyword in this CSV format; leave blank (builder will fall back).
      // Category keyword (FNAM) is intentionally NOT read from this CSV.
      enc := EncodeCobjRecipe(outCountStr, '', '', compsPairsCSV);

      // Parse indices; -1 sentinel means "not specified" but we must reject non-blank junk.
      objTypeIdx := SafeInt(objTypeStr, -1);
      objNameIdx := SafeInt(objNameStr, -1);
      slotIdx    := SafeInt(slotStr, -1);
      optIdx     := SafeInt(optStr, -1);

      if not CobjRecipe_IsValidSpecifiedIdx(objTypeStr, objTypeIdx) then Continue;
      if not CobjRecipe_IsValidSpecifiedIdx(objNameStr, objNameIdx) then Continue;
      if not CobjRecipe_IsValidSpecifiedIdx(slotStr,    slotIdx)    then Continue;
      if not CobjRecipe_IsValidSpecifiedIdx(optStr,     optIdx)     then Continue;

      // Build mask from specified (non-blank) fields.
      mask := 0;
      if Trim(objTypeStr) <> '' then mask := mask or 8;
      if Trim(objNameStr) <> '' then mask := mask or 4;
      if Trim(slotStr)    <> '' then mask := mask or 2;
      if Trim(optStr)     <> '' then mask := mask or 1;

      if mask = 0 then begin
        gCobjRecipeDefault := enc;
        gCobjRecipesHasDefaultRow := True;
        Continue;
      end;

      InitCobjRecipeMaps;
      key := CobjRecipeKeyForMask(mask, objTypeIdx, objNameIdx, slotIdx, optIdx);
      gCobjRecipeByMask[mask].Values[key] := enc;
    end;

    gCobjRecipesLoaded := True;
    Result := True;
  finally
    sl.Free;
    slLines.Free;
  end;
end;


function NormalizeFile(e: IInterface): IInterface;
var
  f: IInterface;
begin
  Result := e;
  if not Assigned(e) then Exit;
  f := nil;
  try
    f := GetFile(e);
  except
    f := nil;
  end;
  if Assigned(f) then
    Result := f;
end;

function FindFileByExactName(const fn: string): IInterface;
var
  i: integer;
  f: IInterface;
begin
  Result := nil;
  for i := 0 to FileCount - 1 do begin
    f := FileByIndex(i);
    if not Assigned(f) then Continue;
    if (LowerCase(GetFileName(f)) = LowerCase(fn)) then begin
      Result := f;
      Exit;
    end;
  end;
end;

function GetTemplateRecordFromFile(const fileName: string; const sig: string): IInterface;
var
  f, grp: IInterface;
begin
  Result := nil;
  f := FindFileByExactName(fileName);
  if not Assigned(f) then Exit;
  grp := GroupBySignature(f, sig);
  if not Assigned(grp) then Exit;
  if ElementCount(grp) < 1 then Exit;
  Result := ElementByIndex(grp, 0);
end;


function EnsureGroup(targetFile: IInterface; sig: string): IInterface;
var
  grp: IInterface;
begin
  Result := nil;
  if not Assigned(targetFile) then Exit;

  targetFile := NormalizeFile(targetFile);

  grp := GroupBySignature(targetFile, sig);
  if not Assigned(grp) then begin
    // Trigger creation; Add() may return a group or a record depending on xEdit, so re-query.
    Add(targetFile, sig, True);
    grp := GroupBySignature(targetFile, sig);
  end;

  Result := grp;
end;
function FindRecordInTargetByEdid(targetFile: IInterface; sig: string; edid: string): IInterface;
var
  grp: IInterface;
begin
  Result := nil;
  if not Assigned(targetFile) then Exit;

  targetFile := NormalizeFile(targetFile);

  grp := GroupBySignature(targetFile, sig);
  if not Assigned(grp) then Exit;
  Result := MainRecordByEditorID(grp, edid);
end;
function FindRecordByEditorIDInFiles(sig: string; edid: string): IInterface;
var
  i, j: integer;
  f, grp, rec: IInterface;
  want, cur: string;
begin
  Result := nil;
  edid := Trim(edid);
  if edid = '' then Exit;

  want := LowerCase(edid);

  for i := 0 to FileCount - 1 do begin
    f := FileByIndex(i);
    grp := GroupBySignature(f, sig);
    if not Assigned(grp) then Continue;

    // Fast exact lookup
    rec := MainRecordByEditorID(grp, edid);
    if Assigned(rec) then begin
      Result := rec;
      Exit;
    end;

    // Fallback: case-insensitive scan (only when exact lookup fails)
    for j := 0 to ElementCount(grp) - 1 do begin
      rec := ElementByIndex(grp, j);
      try
        cur := GetElementEditValues(rec, 'EDID');
      except
        cur := '';
      end;
      if (cur <> '') and (LowerCase(cur) = want) then begin
        Result := rec;
        Exit;
      end;
    end;
  end;
end;

function FindRecordByEdidInAnySig(const aEdid: string): IInterface;
var
  i, j: integer;
  f, grp, rec: IInterface;
begin
  Result := nil;
  if Trim(aEdid) = '' then Exit;

  // Some xEdit builds support MainRecordByEditorID(file, edid); others don't.
  // So we try file-level lookup, then fall back to scanning each top-level group in the file.
  for i := 0 to FileCount - 1 do begin
    f := FileByIndex(i);
    if not Assigned(f) then Continue;

    // Fast path: file-level lookup (if supported)
    rec := nil;
    try
      rec := MainRecordByEditorID(f, aEdid);
    except
      rec := nil;
    end;
    if Assigned(rec) then begin
      Result := rec;
      Exit;
    end;

    // Fallback: scan groups in this file and try group-level lookup
    for j := 0 to ElementCount(f) - 1 do begin
      grp := ElementByIndex(f, j);
      if not Assigned(grp) then Continue;

      // Reduce noise: only try group elements when possible
      // (Signature(grp) is usually 'GRUP' for groups)
      try
        if Signature(grp) <> 'GRUP' then Continue;
      except
        // If Signature throws, skip
        Continue;
      end;

      rec := nil;
      try
        rec := MainRecordByEditorID(grp, aEdid);
      except
        rec := nil;
      end;

      if Assigned(rec) then begin
        Result := rec;
        Exit;
      end;
    end;
  end;
end;
// ============================================================================
// [FNAM/BNAM] Reliable setter: deep traversal to first editable leaf
// Some FO4 elements (notably FNAM) can appear as a container with a null leaf;
// writing to the signature directly may not update the underlying leaf.
// This helper finds the first editable leaf under the signature element and
// writes the desired EditorID / link text there.
// ============================================================================
function SetFirstEditableLeafEditValue(aElem: IInterface; const aValue: string): Boolean;
var
  i: Integer;
  child: IInterface;
begin
  Result := False;
  if not Assigned(aElem) then Exit;

  // Leaf element
  if ElementCount(aElem) = 0 then begin
    try
      SetEditValue(aElem, aValue);
      Result := True;
    except
      Result := False;
    end;
    Exit;
  end;

  // Depth-first: first child leaf that accepts SetEditValue
  for i := 0 to ElementCount(aElem) - 1 do begin
    child := ElementByIndex(aElem, i);
    if not Assigned(child) then Continue;

    if ElementCount(child) = 0 then begin
      try
        SetEditValue(child, aValue);
        Result := True;
        Exit;
      except
        // keep searching
      end;
    end else begin
      if SetFirstEditableLeafEditValue(child, aValue) then begin
        Result := True;
        Exit;
      end;
    end;
  end;
end;

function SetSigFirstEditableLeafEditValue(aRec: IInterface; const aSig, aValue: string): Boolean;
var
  e: IInterface;
begin
  Result := False;
  if not Assigned(aRec) then Exit;

  e := ElementBySignature(aRec, aSig);
  if not Assigned(e) then
    e := Add(aRec, aSig, True);

  if not Assigned(e) then Exit;

  Result := SetFirstEditableLeafEditValue(e, aValue);
end;


function GetOrCreateRecordInTarget(targetFile: IInterface; sig: string; edid: string): IInterface;
var
  grp: IInterface;
  rec: IInterface;
  template: IInterface;
  fn: string;
begin
  Result := nil;
  if not Assigned(targetFile) then Exit;

  targetFile := NormalizeFile(targetFile);

  rec := FindRecordInTargetByEdid(targetFile, sig, edid);
  if Assigned(rec) then begin
    Result := rec;
    Exit;
  end;

  grp := EnsureGroup(targetFile, sig);
  if not Assigned(grp) then begin
    try
      fn := GetFileName(targetFile);
    except
      fn := '<unknown file>';
    end;
    AddMessage('ERROR: EnsureGroup failed for sig=' + sig + ' target=' + fn);
    Exit;
  end;

  rec := Add(grp, sig, True);

  // Fallback #1: try adding directly to file container.
  if not Assigned(rec) then
    rec := Add(targetFile, sig, True);

  // Fallback #2: if Add() fails for this record type, clone a template from Fallout4.esm (first record found).
  if not Assigned(rec) then begin
    template := GetTemplateRecordFromFile('Fallout4.esm', sig);
    if Assigned(template) then begin
      rec := wbCopyElementToFile(template, targetFile, True, True);
    end;
  end;

  if not Assigned(rec) then begin
    AddMessage('ERROR: Could not create record sig=' + sig + ' edid=' + edid);
    Exit;
  end;

  try
    SetElementEditValues(rec, 'EDID', edid);
  except
    AddMessage('ERROR: Failed to set EDID on new ' + sig + ': ' + edid);
    Exit;
  end;

  Result := rec;
end;

// FULL gate helpers
function GetFullText(rec: IInterface): string;
begin
  Result := '';
  if not Assigned(rec) then Exit;
  try
    Result := GetElementEditValues(rec, 'FULL');
  except
    Result := '';
  end;
end;

function IsFullGated(rec: IInterface): boolean;
var
  cur: string;
begin
  cur := GetFullText(rec);
  Result := StartsWithI(cur, fullGatePrefix);
end;

procedure EnsureFullGate(rec: IInterface);
var
  cur, edid: string;
begin
  if not Assigned(rec) then Exit;

  cur := GetFullText(rec);
  if StartsWithI(cur, fullGatePrefix) then Exit;

  edid := GetElementEditValues(rec, 'EDID');
  if cur = '' then
    SetElementEditValues(rec, 'FULL', fullGatePrefix + edid)
  else
    SetElementEditValues(rec, 'FULL', fullGatePrefix + cur);
end;

procedure SetFullIfGated(rec: IInterface; newFull: string);
begin
  if not Assigned(rec) then Exit;
  if not IsFullGated(rec) then Exit;
  try
    SetElementEditValues(rec, 'FULL', newFull);
  except
    // ignore
  end;
end;

// ============================================================================
// [REC] KYWD helpers
// ============================================================================
function EnsureKYWDInTarget(targetFile: IInterface; edid: string; tnam: string): IInterface;
var
  rec: IInterface;
begin
  rec := FindRecordInTargetByEdid(targetFile, 'KYWD', edid);
  if not Assigned(rec) then begin
    rec := GetOrCreateRecordInTarget(targetFile, 'KYWD', edid);

    try
      if Trim(tnam) <> '' then
        SetElementEditValues(rec, 'TNAM', tnam);
    except
      // ignore
    end;
    // New KYWDs always start gated
    EnsureFullGate(rec);
    if verboseDebug then AddMessage('Created KYWD ' + edid);
  end else begin
    // Existing KYWD: do not rebuild structure; FULL is only touched if already gated
    if IsFullGated(rec) then
      EnsureFullGate(rec);
  end;

  Result := rec;
end;

// ============================================================================
// [REC] MISC template copy
// ============================================================================
function GetRecordByFormIDFromFile(FileName, FormID: string): IInterface;
var
  eFile: IInterface;
  eRecord: IInterface;
  clamp: cardinal;
  fixedid: cardinal;
begin
  Result := nil;

  eFile := FileByName(FileName);
  if not Assigned(eFile) then Exit;

  if GetIsESL(eFile) then
    clamp := $FFF
  else
    clamp := $FFFFFF;

  fixedid := (MasterCount(eFile) shl 24) + (StrToInt64('$' + FormID) and clamp);
  eRecord := RecordByFormID(eFile, fixedid, True);
  if not Assigned(eRecord) then Exit;

  Result := eRecord;
end;

function EnsureLooseModMISC(targetFile: IInterface; miscEdid: string): IInterface;
var
  existing: IInterface;
  template: IInterface;
  rec: IInterface;
begin
  Result := nil;
  if not Assigned(targetFile) then Exit;

  existing := FindRecordInTargetByEdid(targetFile, 'MISC', miscEdid);
  if Assigned(existing) then begin
    Result := existing;
    // honor FULL gate rule
    if IsFullGated(existing) then EnsureFullGate(existing);
    Exit;
  end;

  // template: Fallout4.esm Loose Mod
  template := GetRecordByFormIDFromFile('Fallout4.esm', '001A8A41');
  if not Assigned(template) then begin
    AddMessage('ERROR: Could not find template MISC 001A8A41 in Fallout4.esm');
    Exit;
  end;

  rec := wbCopyElementToFile(template, targetFile, True, True);
  SetElementEditValues(rec, 'EDID', miscEdid);
  EnsureFullGate(rec);
  Result := rec;
end;

// ============================================================================
// [REC] MSWP rebuild
// ============================================================================
procedure ClearContainerElements(container: IInterface);
begin
  if not Assigned(container) then Exit;
  while ElementCount(container) > 0 do
    RemoveByIndex(container, 0, True);
end;

procedure RebuildMSWPSubstitutions(mswpRec: IInterface; baseList: TStringList; destList: TStringList; useDefault: boolean;
  objTypeToken: string; objNameToken: string; slotStr: string; criStr: string);
var
  swaps, swap: IInterface;
  i: integer;
  baseMat, destMat: string;
begin
  if not Assigned(mswpRec) then Exit;

  swaps := ElementByPath(mswpRec, 'Material Substitutions');
  if not Assigned(swaps) then
    swaps := Add(mswpRec, 'Material Substitutions', True);

  ClearContainerElements(swaps);

  if useDefault then begin
    baseMat := objTypeToken + '\' + objNameToken + '\' + objNameToken + '_slot' + slotStr + '.bgsm';
    destMat := baseMat;

    swap := ElementAssign(swaps, HighInteger, nil, False);
    SetElementEditValues(swap, 'BNAM', baseMat);
    SetElementEditValues(swap, 'SNAM', destMat);
    if Trim(criStr) <> '' then
      SetElementEditValues(swap, 'CNAM', criStr);

    Exit;
  end;

  for i := 0 to baseList.Count - 1 do begin
    baseMat := baseList[i];
    destMat := destList[i];

    swap := ElementAssign(swaps, HighInteger, nil, False);
    SetElementEditValues(swap, 'BNAM', baseMat);
    SetElementEditValues(swap, 'SNAM', destMat);
    if Trim(criStr) <> '' then
      SetElementEditValues(swap, 'CNAM', criStr);
  end;
end;

// @anchor REC_EnsureAndRebuildMSWP
function EnsureAndRebuildMSWP(targetFile: IInterface; mswpEdid: string; baseList: TStringList; destList: TStringList; useDefault: boolean;
  objTypeToken: string; objNameToken: string; slotStr: string; criStr: string): IInterface;
var
  rec: IInterface;
begin
  Result := nil;
  if not Assigned(targetFile) then Exit;

  // Normalize: keep file interface when already a file.
  targetFile := NormalizeFile(targetFile);
rec := GetOrCreateRecordInTarget(targetFile, 'MSWP', mswpEdid);
  if not Assigned(rec) then begin
    AddMessage('ERROR: EnsureAndRebuildMSWP could not get/create MSWP: ' + mswpEdid);
    Exit;
  end;
  // ALWAYS rebuild swap data
  RebuildMSWPSubstitutions(rec, baseList, destList, useDefault, objTypeToken, objNameToken, slotStr, criStr);
  Result := rec;
end;
// ============================================================================
// [REC] OMOD / COBJ rebuild
// ============================================================================
function CanonicalFormTypeFromObjTypeToken(token: string): string;
var
  t: string;
begin
  t := LowerCase(Trim(token));
  // Normalize common synonyms/plurals
  if t = 'clothes' then t := 'armor';
  if (t = 'armors') then t := 'armor';
  if (t = 'weapons') or (t = 'wpn') or (t = 'weap') then t := 'weapon';

  // Canonical xEdit enum values for OMOD\DATA\Form Type
  if t = 'armor' then begin Result := 'Armor'; Exit; end;
  if t = 'weapon' then begin Result := 'Weapon'; Exit; end;
  if t = 'misc' then begin Result := 'Misc'; Exit; end;
  if t = 'robotmod' then begin Result := 'RobotMod'; Exit; end;
  // fallback: use token directly (may or may not be valid in xEdit)
  Result := Trim(token);
end;

procedure ClearOMODProperties(dataElem: IInterface);
var
  props: IInterface;
begin
  if not Assigned(dataElem) then Exit;
  props := ElementByPath(dataElem, 'Properties');
  if not Assigned(props) then
    props := Add(dataElem, 'Properties', True);
  ClearContainerElements(props);
end;

// @anchor REC_

// -----------------------------------------------------------------------------
// [STAT/MSTT] Apply MSWPs / Color Remap Index to duplicated placeable variants
//
// STAT example (CrateLargeAqua):
//   Model\MODS - Material Swap   (sig MODS)
//   Model\MODC - Color Remapping Index (sig MODC)
//
// MSTT example with destruction stages (Truck01Red):
//   ...\Model\DMDS - Material Swap (sig DMDS)  [per stage]
//   ...\Model\DMDC - Color Remapping Index (sig DMDC) [per stage]
//
// We support both by:
//   - setting Model\MODS when present
//   - recursively setting any DMDS / DMDC elements found anywhere in the record
// -----------------------------------------------------------------------------

function DeepSetFirstEditableLeaf(el: IInterface; const value: string): boolean;
var
  i: integer;
  child: IInterface;
begin
  Result := False;
  if not Assigned(el) then Exit;

  // Try setting this node directly.
  try
    SetEditValue(el, value);
    Result := True;
    Exit;
  except
    // Not editable here; fall through.
  end;

  // Otherwise recurse into children until we find the first editable leaf.
  for i := 0 to ElementCount(el) - 1 do begin
    child := ElementByIndex(el, i);
    if DeepSetFirstEditableLeaf(child, value) then begin
      Result := True;
      Exit;
    end;
  end;
end;

procedure RecurseSetEditValueBySignature(aRoot: IInterface; const aSig: string; const aValue: string; var aCount: integer);
var
  i: integer;
  e: IInterface;
begin
  if not Assigned(aRoot) then Exit;

  try
    if Signature(aRoot) = aSig then begin
      if DeepSetFirstEditableLeaf(aRoot, aValue) then
        Inc(aCount);
      Exit;
    end;
  except
    // ignore
  end;

  if ElementCount(aRoot) <= 0 then Exit;

  for i := 0 to ElementCount(aRoot) - 1 do begin
    e := ElementByIndex(aRoot, i);
    if Assigned(e) then
      RecurseSetEditValueBySignature(e, aSig, aValue, aCount);
  end;
end;


// Fallback when Signature() on nested leaf isn't reliable: match by display Name() prefix.
procedure RecurseSetEditValueByNamePrefix(aRoot: IInterface; const aPrefix: string; const aValue: string; var aCount: integer);
var
  i: integer;
  e: IInterface;
  n: string;
begin
  if not Assigned(aRoot) then Exit;

  n := '';
  try
    n := Name(aRoot);
  except
    n := '';
  end;

  if (n <> '') and (Pos(aPrefix, n) = 1) then begin
    if DeepSetFirstEditableLeaf(aRoot, aValue) then
      Inc(aCount);
  end;

  if ElementCount(aRoot) <= 0 then Exit;

  for i := 0 to ElementCount(aRoot) - 1 do begin
    e := ElementByIndex(aRoot, i);
    if Assigned(e) then
      RecurseSetEditValueByNamePrefix(e, aPrefix, aValue, aCount);
  end;
end;


function FindModelModsElementIfPresent(aRec: IInterface): IInterface;
begin
  Result := nil;
  if not Assigned(aRec) then Exit;

  // STAT/MSTT simple model path (most common)
  Result := ElementByPath(aRec, 'Model\MODS - Material Swap');
  if Assigned(Result) then Exit;

  // Common fallbacks seen across records
  Result := ElementByPath(aRec, 'Model\MODS');
  if Assigned(Result) then Exit;

  Result := ElementByPath(aRec, 'Model\MODS - Material Swaps');
  if Assigned(Result) then Exit;

  Result := ElementByPath(aRec, 'Model\Material Swaps');
  if Assigned(Result) then Exit;
end;


function FindChildByNamePrefix(aCont: IInterface; const aPrefix: string): IInterface;
var
  i: integer;
  e: IInterface;
  n: string;
begin
  Result := nil;
  if not Assigned(aCont) then Exit;
  if ElementCount(aCont) <= 0 then Exit;

  for i := 0 to ElementCount(aCont) - 1 do begin
    e := ElementByIndex(aCont, i);
    if not Assigned(e) then Continue;
    try
      n := Name(e);
    except
      n := '';
    end;
    if (n <> '') and (Pos(aPrefix, n) = 1) then begin
      Result := e;
      Exit;
    end;
  end;
end;

function EnsureChildBySig(aCont: IInterface; const aSig: string): IInterface;
begin
  Result := FindChildByNamePrefix(aCont, aSig);
  if Assigned(Result) then Exit;

  try
    Result := Add(aCont, aSig, True);
  except
    Result := nil;
  end;
end;

procedure BuildMSWPRefStrings(aMSWP: IInterface; var outName, outEdid, outForm: string);
begin
  outName := '';
  outEdid := '';
  outForm := '';

  if not Assigned(aMSWP) then Exit;

  try
    outEdid := Trim(GetElementEditValues(aMSWP, 'EDID'));
  except
    outEdid := '';
  end;

  try
    outName := Name(aMSWP);
  except
    outName := '';
  end;

  // Name() is usually "EDID [SIG:FORMID]" -> strip trailing bracket section if present
  if Pos(' [', outName) > 0 then
    outName := Copy(outName, 1, Pos(' [', outName) - 1);

  try
    outForm := IntToHex(GetLoadOrderFormID(aMSWP), 8);
  except
    outForm := '';
  end;
end;

function TrySetRefValueDeep(aEl: IInterface; const v1, v2, v3: string): boolean;
begin
  Result := False;
  if not Assigned(aEl) then Exit;

  if (v1 <> '') and DeepSetFirstEditableLeaf(aEl, v1) then begin
    Result := True;
    Exit;
  end;
  if (v2 <> '') and DeepSetFirstEditableLeaf(aEl, v2) then begin
    Result := True;
    Exit;
  end;
  if (v3 <> '') and DeepSetFirstEditableLeaf(aEl, v3) then begin
    Result := True;
    Exit;
  end;
end;

procedure ApplySwapToModelContainer(aModel: IInterface; const swapSig: string; const v1, v2, v3: string; var aCount: integer);
var
  el: IInterface;
begin
  if not Assigned(aModel) then Exit;

  el := FindChildByNamePrefix(aModel, swapSig);
  if not Assigned(el) then
    el := EnsureChildBySig(aModel, swapSig);

  if Assigned(el) then begin
    if TrySetRefValueDeep(el, v1, v2, v3) then
      Inc(aCount);
  end;
end;

procedure ApplyCRIToModelContainer(aModel: IInterface; const criSig: string; const s: string; var aCount: integer);
var
  el: IInterface;
begin
  if not Assigned(aModel) then Exit;

  el := FindChildByNamePrefix(aModel, criSig);
  if not Assigned(el) then
    el := EnsureChildBySig(aModel, criSig);

  if Assigned(el) then begin
    if DeepSetFirstEditableLeaf(el, s) then
      Inc(aCount);
  end;
end;

function IsStageModelContainer(aCont: IInterface): boolean;
begin
  Result := False;
  if not Assigned(aCont) then Exit;

  // Stage model containers hold DMDL/DMDT subrecords.
  if Assigned(FindChildByNamePrefix(aCont, 'DMDL')) then begin
    Result := True;
    Exit;
  end;
  if Assigned(FindChildByNamePrefix(aCont, 'DMDT')) then begin
    Result := True;
    Exit;
  end;
end;

procedure RecurseApplyToStageModelContainers(aRoot: IInterface; const doSwap, doCRI: boolean;
  const v1, v2, v3: string; const criStr: string; var swapCount: integer; var criCount: integer);
var
  i: integer;
  child: IInterface;
begin
  if not Assigned(aRoot) then Exit;

  if IsStageModelContainer(aRoot) then begin
    if doSwap then
      ApplySwapToModelContainer(aRoot, 'DMDS', v1, v2, v3, swapCount);
    if doCRI then
      ApplyCRIToModelContainer(aRoot, 'DMDC', criStr, criCount);
  end;

  if ElementCount(aRoot) <= 0 then Exit;
  for i := 0 to ElementCount(aRoot) - 1 do begin
    child := ElementByIndex(aRoot, i);
    if Assigned(child) then
      RecurseApplyToStageModelContainers(child, doSwap, doCRI, v1, v2, v3, criStr, swapCount, criCount);
  end;
end;


procedure SetModelMSWP(aRec, aMSWP: IInterface);
var
  model: IInterface;
  mswpName, mswpEdid, mswpForm: string;
  swapCount, dummyCri: integer;
begin
  if (not Assigned(aRec)) or (not Assigned(aMSWP)) then Exit;

  BuildMSWPRefStrings(aMSWP, mswpName, mswpEdid, mswpForm);

  swapCount := 0;
  dummyCri := 0;

  // STAT-style: Model\MODS (create if missing)
  model := ElementByPath(aRec, 'Model');
  if Assigned(model) then
    ApplySwapToModelContainer(model, 'MODS', mswpName, mswpEdid, mswpForm, swapCount);

  // MSTT destruct stage models: Model\DMDS (create if missing)
  RecurseApplyToStageModelContainers(aRec, True, False, mswpName, mswpEdid, mswpForm, '', swapCount, dummyCri);

  if swapCount = 0 then
    AddMessage('WARN: No MODS/DMDS fields found (or creatable) to set MSWP on ' + Name(aRec));
end;



procedure SetModelCRI(aRec: IInterface; aCRI: double);
var
  model: IInterface;
  s: string;
  criCount, dummySwap: integer;
begin
  if not Assigned(aRec) then Exit;

  s := FloatToStrDot(aCRI);

  criCount := 0;
  dummySwap := 0;

  // STAT-style: Model\MODC (create if missing)
  model := ElementByPath(aRec, 'Model');
  if Assigned(model) then
    ApplyCRIToModelContainer(model, 'MODC', s, criCount);

  // MSTT destruct stage models: Model\DMDC (create if missing)
  RecurseApplyToStageModelContainers(aRec, False, True, '', '', '', s, dummySwap, criCount);

  if criCount = 0 then
    AddMessage('WARN: No MODC/DMDC fields found (or creatable) to set CRI on ' + Name(aRec));
end;



function EnsureAndRebuildOMOD(targetFile: IInterface; omodEdid: string; formType: string; apKywd: IInterface; maKywd: IInterface; mswpRec: IInterface;
  wantLooseMisc: boolean; miscEdidOut: string): IInterface;
var
  rec, dataElem, propArray, prop, mnamElem: IInterface;
  miscRec: IInterface;
  curFull: string;
begin
  Result := nil;
  if not Assigned(targetFile) then Exit;

  rec := GetOrCreateRecordInTarget(targetFile, 'OMOD', omodEdid);

  // Respect FULL gating rule: do not touch FULL if not gated.
  curFull := GetFullText(rec);
  if (curFull = '') or IsFullGated(rec) then
    EnsureFullGate(rec);

  // Core fields
  try SetElementEditValues(rec, 'DESC', ''); except end;

  // DATA
  dataElem := ElementByPath(rec, 'DATA');
  if not Assigned(dataElem) then
    dataElem := Add(rec, 'DATA', True);

  // OMOD\DATA\Form Type is an enum. If the caller passes a non-canonical token
  // (e.g. "weapons"), xEdit will ignore it and keep the prior value. We set the
  // canonical enum string (Armor/Weapon/...) and warn if it doesn't stick.
  try SetElementEditValues(dataElem, 'Form Type', formType); except end;
  try
    if LowerCase(GetElementEditValues(dataElem, 'Form Type')) <> LowerCase(formType) then
      AddMessage('WARNING: OMOD Form Type not set for ' + omodEdid + ' (wanted ' + formType + ', got ' + GetElementEditValues(dataElem, 'Form Type') + ')');
  except
    // ignore
  end;
  try SetElementEditValues(dataElem, 'Attach Point', Name(apKywd)); except end;

  ClearOMODProperties(dataElem);
  propArray := ElementByPath(dataElem, 'Properties');
  prop := ElementAssign(propArray, HighInteger, nil, False);
  SetElementEditValues(prop, 'Value Type', 'FormID,Int');
  SetElementEditValues(prop, 'Function Type', 'ADD');
  SetElementEditValues(prop, 'Property', 'MaterialSwaps');
  SetElementEditValues(prop, 'Value 1 - FormID', Name(mswpRec));

  // MNAM
  mnamElem := ElementByPath(rec, 'MNAM');
  if not Assigned(mnamElem) then
    mnamElem := Add(rec, 'MNAM', True);
  try SetElementEditValues(mnamElem, 'Keyword', Name(maKywd)); except end;

  // Optional loose misc
  if wantLooseMisc then begin
    miscRec := EnsureLooseModMISC(targetFile, miscEdidOut);
    if Assigned(miscRec) then begin
      try SetElementEditValues(rec, 'LNAM', Name(miscRec)); except end;
    end;
  end;

  Result := rec;
end;

// @anchor REC_EnsureAndRebuildCOBJ
function EnsureAndRebuildCOBJ(targetFile: IInterface; cobjEdid: string; createdRec: IInterface; recipeEnc: string): IInterface;
var
  rec, intv, items, entry: IInterface;
  wbKywd: IInterface;
  catKywd: IInterface;
  fnamKywd: IInterface;
  bnamKywd: IInterface;
  outCount: integer;
  wbKywdEdid, catKywdEdid, compsPairsCSV: string;
  tmp: TStringList;
  i, n: integer;
  compName, compCountStr: string;
  compRec: IInterface;
  didSetBNAM: boolean;
  didSetFNAM: boolean;
begin
  Result := nil;
  if not Assigned(targetFile) then Exit;

  rec := GetOrCreateRecordInTarget(targetFile, 'COBJ', cobjEdid);

  // Minimal deterministic fields
  try SetElementEditValues(rec, 'DESC', ''); except end;
  try SetElementEditValues(rec, 'CNAM', Name(createdRec)); except end;

  // Default workbench (BNAM). Can be overridden by recipeEnc.
  wbKywd := FindRecordByEditorIDInFiles('KYWD', 'WorkbenchChemlab');
  
  //Default category (FNAM). Can be overridden by recipeEnc.
  catKywd := FindRecordByeditorIDInFiles('KYWD', 'RecipeUtility');

  // Optional recipe override (from cobjrecipes.csv)
  // Encoding: OutCount|WorkbenchKywd|CategoryKywd|Comp1,Count1,Comp2,Count2,...
  outCount := 1;
  wbKywdEdid := '';
  catKywdEdid := '';
  compsPairsCSV := '';
  if Trim(recipeEnc) <> '' then begin
    tmp := TStringList.Create;
    try
      tmp.StrictDelimiter := True;
      tmp.Delimiter := '|';
      tmp.DelimitedText := recipeEnc;
      if tmp.Count > 0 then outCount := SafeInt(tmp[0], 1);
      if tmp.Count > 1 then wbKywdEdid := TrimOrEmpty(tmp[1]);
      if tmp.Count > 2 then catKywdEdid := TrimOrEmpty(tmp[2]);
      if tmp.Count > 3 then compsPairsCSV := TrimOrEmpty(tmp[3]);
    finally
      tmp.Free;
    end;

    // When building alongside OMODs, COBJ workbench/category assignment is unnecessary.
    // Skip BNAM/FNAM writes in that mode.
    if not gDoBuildOMOD then begin

    // BNAM (workbench keyword)
    if wbKywdEdid <> '' then begin
      bnamKywd := FindRecordByEditorIDInFiles('KYWD', wbKywdEdid);
      if Assigned(bnamKywd) then
        try
          didSetBNAM := SetSigFirstEditableLeafEditValue(rec, 'BNAM', Name(bnamKywd));
          if not didSetBNAM then
            AddMessage('WARNING: Failed to set BNAM (deep leaf) for COBJ ' + cobjEdid);
        except end
      else
        AddMessage('WARNING: Could not find KYWD "' + wbKywdEdid + '" for COBJ BNAM: ' + cobjEdid);
    end;

    // FNAM (category keyword)
    if catKywdEdid <> '' then begin
      fnamKywd := FindRecordByEditorIDInFiles('KYWD', catKywdEdid);
      if Assigned(fnamKywd) then
        try
          didSetFNAM := SetSigFirstEditableLeafEditValue(rec, 'FNAM', Name(fnamKywd));
          if not didSetFNAM then
            AddMessage('WARNING: Failed to set FNAM (deep leaf) for COBJ ' + cobjEdid);
        except end
      else
        AddMessage('WARNING: Could not find KYWD "' + catKywdEdid + '" for COBJ FNAM: ' + cobjEdid);
    end;
    end;
  end;

  if not gDoBuildOMOD then begin
  // Apply default BNAM if not overridden
  if not didSetBNAM then begin
    if Assigned(wbKywd) then begin
      try
        if (Trim(GetElementEditValues(rec, 'BNAM')) = '') or
           (Pos('NULL', UpperCase(GetElementEditValues(rec, 'BNAM'))) > 0) then begin
          if not SetSigFirstEditableLeafEditValue(rec, 'BNAM', Name(wbKywd)) then
            AddMessage('WARNING: Failed to set default BNAM (deep leaf) for COBJ ' + cobjEdid);
        end;
      except
        // ignore
      end;
    end else
      AddMessage('WARNING: Could not find KYWD WorkbenchChemlab (default BNAM) in loaded files.');
  end;

  // Apply default FNAM if not overridden
  if not didSetFNAM then begin
    if Assigned(catKywd) then begin
      try
        if (Trim(GetElementEditValues(rec, 'FNAM')) = '') or
           (Pos('NULL', UpperCase(GetElementEditValues(rec, 'FNAM'))) > 0) then begin
          if not SetSigFirstEditableLeafEditValue(rec, 'FNAM', Name(catKywd)) then
            AddMessage('WARNING: Failed to set default FNAM (deep leaf) for COBJ ' + cobjEdid);
        end;
      except
        // ignore
      end;
    end else
      AddMessage('WARNING: Could not find KYWD RecipeUtility (default FNAM) in loaded files.');
  end;

  end;

  intv := ElementByPath(rec, 'INTV');
  if not Assigned(intv) then
    intv := Add(rec, 'INTV', True);
  try SetElementEditValues(intv, 'Created Object Count', IntToStr(outCount)); except end;

  // Components / ingredient list (only rebuilt when a recipe was provided)
  if Trim(recipeEnc) <> '' then begin
    items := ElementBySignature(rec, 'FVPA');
    if not Assigned(items) then
      items := Add(rec, 'FVPA', True);
    ClearContainerElements(items);

    // Special case: "null" means explicitly no components
    if LowerCase(Trim(compsPairsCSV)) <> 'null' then begin
      tmp := TStringList.Create;
      try
        tmp.StrictDelimiter := True;
        tmp.Delimiter := ',';
        tmp.DelimitedText := compsPairsCSV;

        n := 0;
        i := 0;
        while i < tmp.Count do begin
          compName := TrimOrEmpty(tmp[i]);
          compCountStr := TrimOrEmpty(GetCol(tmp, i + 1));
          i := i + 2;

          if compName = '' then Continue;
          if compCountStr = '' then compCountStr := '1';

          compRec := FindRecordByEditorIDInFiles('MISC', compName);
          if not Assigned(compRec) then
            compRec := FindRecordByEditorIDInFiles('CMPO', compName);
          if not Assigned(compRec) then begin
            AddMessage('WARNING: Missing component record for COBJ recipe: ' + compName + ' (COBJ ' + cobjEdid + ')');
            Continue;
          end;

          entry := ElementAssign(items, HighInteger, nil, False);
          // Try common xEdit field names for COBJ ingredients
          try SetElementEditValues(entry, 'Item', Name(compRec)); except end;
          try SetElementEditValues(entry, 'Component', Name(compRec)); except end;
          try SetElementEditValues(entry, 'Count', compCountStr); except end;
          try SetElementEditValues(entry, 'Item Count', compCountStr); except end;
          Inc(n);
        end;

        // COCT count (best-effort; xEdit may derive this)
        try SetElementEditValues(rec, 'COCT', IntToStr(n)); except end;
      finally
        tmp.Free;
      end;
    end else begin
      try SetElementEditValues(rec, 'COCT', '0'); except end;
    end;
  end;

  Result := rec;
end;

// ============================================================================
// [NAM] Rename helper
// ============================================================================
function TryRenameRecord(targetFile: IInterface; sig: string; oldEdid: string; newEdid: string): IInterface;
var
  rec: IInterface;
begin
  Result := nil;
  if oldEdid = newEdid then Exit;

  rec := FindRecordInTargetByEdid(targetFile, sig, oldEdid);
  if not Assigned(rec) then Exit;

  try
    SetElementEditValues(rec, 'EDID', newEdid);
  except
    AddMessage('WARNING: Failed to rename ' + sig + ' EDID ' + oldEdid + ' -> ' + newEdid);
  end;

  Result := rec;
end;

function TryMigrateRecordByEdid(targetFile: IInterface; sig: string; legacyEdid: string; newEdid: string): IInterface;
var
  recNew, recOld: IInterface;
begin
  Result := nil;
  if (legacyEdid = '') or (newEdid = '') then Exit;
  if legacyEdid = newEdid then Exit;

  // If the new EDID already exists, do not migrate.
  recNew := FindRecordInTargetByEdid(targetFile, sig, newEdid);
  if Assigned(recNew) then begin
    Result := recNew;
    Exit;
  end;

  recOld := FindRecordInTargetByEdid(targetFile, sig, legacyEdid);
  if not Assigned(recOld) then Exit;

  try
    SetElementEditValues(recOld, 'EDID', newEdid);
    Result := recOld;
  except
    // ignore
    Result := nil;
  end;
end;

// ============================================================================
// [CSV] Row parsing helpers
// ============================================================================
function GetCol(sl: TStringList; idx: integer): string;
begin
  Result := '';
  if not Assigned(sl) then Exit;
  if (idx < 0) or (idx >= sl.Count) then Exit;
  Result := sl[idx];
end;


function IsBuildHeaderRow(slRow: TStringList): boolean;
var
  c0, c1, c2: string;
begin
  Result := False;
  if not Assigned(slRow) then Exit;
  c0 := LowerCase(TrimOrEmpty(GetCol(slRow, 0)));
  c1 := LowerCase(TrimOrEmpty(GetCol(slRow, 1)));
  c2 := LowerCase(TrimOrEmpty(GetCol(slRow, 2)));

  // Typical header variants: ProjectID,ObjTypeStart,NumObjType,...
  if (c0 = 'projectid') then begin
    Result := True;
    Exit;
  end;

  if (Pos('project', c0) > 0) and (Pos('objtype', c1) > 0) then begin
    Result := True;
    Exit;
  end;

  // Another common variant: ProjectID,TypeStart,NumType,...
  if (c0 = 'project') and (Pos('start', c1) > 0) and (Pos('num', c2) > 0) then begin
    Result := True;
    Exit;
  end;
end;
// @anchor LBL_ParsePairLists
procedure ParsePairLists(slRow: TStringList; startCol: integer; baseList: TStringList; destList: TStringList);
var
  colBase, colDest: integer;
  baseVal, destVal: string;
begin
  baseList.Clear;
  destList.Clear;

  colBase := startCol;
  colDest := startCol + 1;

  while colBase < slRow.Count do begin
    baseVal := TrimOrEmpty(GetCol(slRow, colBase));
    if baseVal = '' then Break;

    destVal := TrimOrEmpty(GetCol(slRow, colDest));
    if destVal = '' then destVal := baseVal;

    baseList.Add(baseVal);
    destList.Add(destVal);

    colBase := colBase + 2;
    colDest := colDest + 2;
  end;
end;

// ============================================================================
// [LBL] label.csv optional BGSM pair overrides
// ============================================================================
function ExpandBGSMTemplate(const sIn: string; const objTypeTokenBGSM: string; const objNameToken: string; const slotStr: string; const optStr: string): string;
var
  s: string;
begin
  s := sIn;

  // Support both {TOKEN} and %TOKEN% styles (case-insensitive).
  s := StringReplace(s, '{OBJTYPE}', objTypeTokenBGSM, [rfReplaceAll, rfIgnoreCase]);
  s := StringReplace(s, '{OBJNAME}', objNameToken, [rfReplaceAll, rfIgnoreCase]);
  s := StringReplace(s, '{SLOT}',    slotStr,        [rfReplaceAll, rfIgnoreCase]);
  s := StringReplace(s, '{OPT}',     optStr,         [rfReplaceAll, rfIgnoreCase]);

  s := StringReplace(s, '%OBJTYPE%', objTypeTokenBGSM, [rfReplaceAll, rfIgnoreCase]);
  s := StringReplace(s, '%OBJNAME%', objNameToken,     [rfReplaceAll, rfIgnoreCase]);
  s := StringReplace(s, '%SLOT%',    slotStr,          [rfReplaceAll, rfIgnoreCase]);
  s := StringReplace(s, '%OPT%',     optStr,           [rfReplaceAll, rfIgnoreCase]);

  Result := s;
end;

procedure ExpandBGSMListTokens(list: TStringList; const objTypeTokenBGSM: string; const objNameToken: string; const slotStr: string; const optStr: string);
var
  i: integer;
begin
  if not Assigned(list) then Exit;
  for i := 0 to list.Count - 1 do
    list[i] := ExpandBGSMTemplate(list[i], objTypeTokenBGSM, objNameToken, slotStr, optStr);
end;

procedure ParsePairListsFromCSVString(const pairsCSV: string; baseList: TStringList; destList: TStringList);
var
  tmp: TStringList;
begin
  baseList.Clear;
  destList.Clear;

  if not PairsCSVHasData(pairsCSV) then Exit;

  tmp := TStringList.Create;
  try
    tmp.StrictDelimiter := True;
    tmp.Delimiter := ',';
    tmp.DelimitedText := pairsCSV;
    ParsePairLists(tmp, 0, baseList, destList);
  finally
    tmp.Free;
  end;
end;

// @anchor LBL_ResolvePairsCSVForMSWP
function ResolvePairsCSVForMSWP(objTypeIdx: integer; objNameIdx: integer; slotIdx: integer; optIdx: integer; numObjType: integer): string;
var
  key, v: string;
begin
  Result := '';

  // 1) Exact per-record override
  if Assigned(gPairsMSWP) then begin
    key := Pad2(objTypeIdx) + Pad2(objNameIdx) + Pad2(slotIdx) + Pad2(optIdx);
    v := gPairsMSWP.Values[key];
    if PairsCSVHasData(v) then begin Result := v; Exit; end;
  end;

  // 2) Per-OPT override
  if Assigned(gPairsOpt) then begin
    key := Pad2(optIdx);
    v := gPairsOpt.Values[key];
    if PairsCSVHasData(v) then begin Result := v; Exit; end;
  end;

  // 3) Per-SLOT override
  if Assigned(gPairsSlot) then begin
    key := Pad2(slotIdx);
    v := gPairsSlot.Values[key];
    if PairsCSVHasData(v) then begin Result := v; Exit; end;
  end;

  // 4) Per-OBJNAME override (prefer scoped TTNN)
  if Assigned(gPairsObjName) then begin
    key := Pad2(objTypeIdx) + Pad2(objNameIdx);
    v := gPairsObjName.Values[key];
    if PairsCSVHasData(v) then begin Result := v; Exit; end;

    if numObjType <= 1 then begin
      key := Pad2(objNameIdx);
      v := gPairsObjName.Values[key];
      if PairsCSVHasData(v) then begin Result := v; Exit; end;
    end;
  end;

  // 5) Per-OBJTYPE override
  if Assigned(gPairsObjType) then begin
    key := Pad2(objTypeIdx);
    v := gPairsObjType.Values[key];
    if PairsCSVHasData(v) then begin Result := v; Exit; end;
  end;
end;

// @anchor LBL_ResolveExactMSWPPairsCSV
function ResolveExactMSWPPairsCSV(objTypeIdx: integer; objNameIdx: integer; slotIdx: integer; optIdx: integer): string;
var
  key, v: string;
begin
  Result := '';
  if not Assigned(gPairsMSWP) then Exit;
  key := Pad2(objTypeIdx) + Pad2(objNameIdx) + Pad2(slotIdx) + Pad2(optIdx);
  v := gPairsMSWP.Values[key];
  if PairsCSVHasData(v) then
    Result := v;
end;

// ============================================================================
// [RUN] Passes: REVERT, BUILD, LABEL
// ============================================================================
// @anchor RUN_RevertPass
procedure RevertPass(buildLines: TStringList);
var
  i: integer;
  slRow: TStringList;
  line, projectId: string;
  objTypeStart, numObjType, objNameStart, numObjName: integer;
  slotStart, numSlot, optStart, numOpt: integer;
    effNumOpt: integer;
  hasOptDim: boolean;
criStartStr, criStepStr: string;
  hasCRI: boolean;
  criStart, criStep, criVal: double;
  baseList, destList: TStringList;
  useDefaultPairs: boolean;
  pairsCSV: string;
  mswpPairsBase, mswpPairsDest: TStringList;

  objTypeOff, objNameOff, slotOff, optOff: integer;
  objTypeIdx, objNameIdx, slotIdx, optIdx: integer;
  objTypeToken, objTypeTokenBGSM, objNameToken: string;
  canonicalSeg, labeledSeg: string;
  slotStr, optStr: string;

  // EDIDs
  oldEdid, newEdid: string;
  maOld, maNew, apOld, apNew: string;
  mswpOld, mswpNew: string;
  omodOld, omodNew: string;
  cobjOld, cobjNew: string;
  miscOld, miscNew: string;

  rec: IInterface;
  isMSWPOnly: boolean;
begin
  slRow := TStringList.Create;
  baseList := TStringList.Create;
  destList := TStringList.Create;
  mswpPairsBase := TStringList.Create;
  mswpPairsDest := TStringList.Create;
  try
    slRow.StrictDelimiter := True;
    slRow.Delimiter := ',';

    isMSWPOnly := gDoBuildMSWP and (not gDoBuildOMOD) and (not gDoBuildCOBJ) and (not gDoBuildMISC);

    for i := 0 to buildLines.Count - 1 do begin
      line := Trim(buildLines[i]);
      if line = '' then Continue;

      // Allow comment lines in build.csv
      if (Copy(line, 1, 1) = '#') or (Copy(line, 1, 1) = ';') then Continue;

      slRow.Clear;
      slRow.DelimitedText := buildLines[i];
      if IsBuildHeaderRow(slRow) then Continue;
      if slRow.Count < 11 then begin
        AddMessage('Skipping build row ' + IntToStr(i+1) + ': not enough columns');
        Continue;
      end;

      projectId := TrimOrEmpty(GetCol(slRow, 0));
      objTypeStart := SafeInt(GetCol(slRow, 1), -1);
      numObjType   := SafeInt(GetCol(slRow, 2), 0);
      objNameStart := SafeInt(GetCol(slRow, 3), -1);
      numObjName   := SafeInt(GetCol(slRow, 4), 0);
      slotStart    := SafeInt(GetCol(slRow, 5), -1);
      numSlot      := SafeInt(GetCol(slRow, 6), 0);
      optStart     := SafeInt(GetCol(slRow, 7), -1);
      numOpt       := SafeInt(GetCol(slRow, 8), 0);

      criStartStr  := TrimOrEmpty(GetCol(slRow, 9));
      criStepStr   := TrimOrEmpty(GetCol(slRow, 10));
      hasCRI := criStartStr <> '';
      criStart := SafeFloat(criStartStr, 0.0);
      criStep  := SafeFloat(criStepStr, 0.0);

      if (projectId = '') or (numObjType <= 0) or (numObjName <= 0) or (numSlot <= 0) or (numOpt < 0) then begin
        AddMessage('Skipping build row ' + IntToStr(i+1) + ': invalid ProjectID or counts');
        Continue;
      end;

      // Opt dimension can be disabled (NumOpt = 0). In that case we run the opt loop once,
      // omit any _optNN suffix from EDIDs, and ignore optStart.
      hasOptDim := (numOpt > 0);
      effNumOpt := numOpt;
      if effNumOpt <= 0 then effNumOpt := 1;
      if not hasOptDim then optStart := 0;

      ParsePairLists(slRow, 11, baseList, destList);
      useDefaultPairs := baseList.Count = 0;
      // MSWP-only: if CRIStart is provided but no Base/Dest pairs are provided,
      // treat this as a CRI-only variant run (do NOT auto-generate default swap pairs).
      if hasCRI and (baseList.Count = 0) and (not gDoBuildOMOD) and (not gDoBuildCOBJ) and (not gDoBuildMISC) then
        useDefaultPairs := False;


      for objTypeOff := 0 to numObjType - 1 do begin
        objTypeIdx := objTypeStart + objTypeOff;
        if (objTypeIdx < 0) or (objTypeIdx > 99) then Continue;
        objTypeToken := GetMapVal('ObjType', objTypeIdx);
        if Trim(objTypeToken) = '' then begin
          AddMessage('ERROR: Missing ObjType token for index ' + Pad2(objTypeIdx) + ' (row ' + IntToStr(i+1) + ')');
          Continue;
        end;

        // EDID uses alias mapping (e.g. clothes -> armor). BGSM defaults use raw token.
        objTypeTokenBGSM := Trim(objTypeToken);
        objTypeToken := ObjTypeTokenForEDID(objTypeToken);

        for objNameOff := 0 to numObjName - 1 do begin
          objNameIdx := objNameStart + objNameOff;
          if (objNameIdx < 0) or (objNameIdx > 99) then Continue;
          objNameToken := GetObjNameToken(objTypeIdx, objNameIdx, numObjType);
          if Trim(objNameToken) = '' then begin
            if numObjType > 1 then
              AddMessage('ERROR: Missing ObjName token for ObjType ' + Pad2(objTypeIdx) + ' and ObjName ' + Pad2(objNameIdx) +
                         ' (expect label.csv: ObjName,' + Pad2(objTypeIdx) + Pad2(objNameIdx) + ',<token>) (row ' + IntToStr(i+1) + ')')
            else
              AddMessage('ERROR: Missing ObjName token for index ' + Pad2(objNameIdx) + ' (row ' + IntToStr(i+1) + ')');
            Continue;
          end;

          canonicalSeg := CanonicalObjSeg(objTypeIdx, objNameIdx, numObjType);
          labeledSeg := objNameToken;

          if not isMSWPOnly then begin
            // MA rename + FULL gate
            maOld := kywdMAPrefix + projectId + '_' + labeledSeg;
            maNew := kywdMAPrefix + projectId + '_' + canonicalSeg;
            rec := TryRenameRecord(gTargetFile, 'KYWD', maOld, maNew);
            if Assigned(rec) then EnsureFullGate(rec);
          end;

          for slotOff := 0 to numSlot - 1 do begin
            slotIdx := slotStart + slotOff;
            if (slotIdx < 0) or (slotIdx > 99) then Continue;
            slotStr := Pad2(slotIdx);

            if not isMSWPOnly then begin
              // AP rename + FULL gate
              apOld := kywdAPPrefix + projectId + '_' + labeledSeg + '_slot' + slotStr;
              apNew := kywdAPPrefix + projectId + '_' + canonicalSeg + '_slot' + slotStr;
              rec := TryRenameRecord(gTargetFile, 'KYWD', apOld, apNew);
              if Assigned(rec) then EnsureFullGate(rec);
            end;

            for optOff := 0 to effNumOpt - 1 do begin
              optIdx := optStart + optOff;
              if hasOptDim then begin
                if (optIdx < 0) or (optIdx > 99) then Continue;
                optStr := Pad2(optIdx);
              end else begin
                // No opt dimension: normalize.
                optIdx := 0;
                optStr := '00';
              end;
// compute CRI for this optOffset
              if hasCRI then
                criVal := criStart + (((slotIdx - slotStart) * effNumOpt + (optIdx - optStart)) * criStep)
              else
                criVal := 0.0;

              // MSWP
              mswpOld := mswpPrefix + objTypeToken + '_' + projectId + '_' + labeledSeg + '_slot' + slotStr + OptSuffix(hasOptDim, optStr);
              mswpNew := mswpPrefix + objTypeToken + '_' + projectId + '_' + canonicalSeg + '_slot' + slotStr + OptSuffix(hasOptDim, optStr);
              TryRenameRecord(gTargetFile, 'MSWP', mswpOld, mswpNew);

              // OMOD
              omodOld := omodPrefix + objTypeToken + '_' + projectId + '_' + labeledSeg + '_slot' + slotStr + OptSuffix(hasOptDim, optStr);
              omodNew := omodPrefix + objTypeToken + '_' + projectId + '_' + canonicalSeg + '_slot' + slotStr + OptSuffix(hasOptDim, optStr);
              rec := TryRenameRecord(gTargetFile, 'OMOD', omodOld, omodNew);
              if Assigned(rec) then EnsureFullGate(rec);

              // COBJ
              cobjOld := cobjPrefix + omodOld;
              cobjNew := cobjPrefix + omodNew;
              TryRenameRecord(gTargetFile, 'COBJ', cobjOld, cobjNew);

              // MISC
              miscOld := miscPrefix + omodOld;
              miscNew := miscPrefix + omodNew;
              rec := TryRenameRecord(gTargetFile, 'MISC', miscOld, miscNew);
              if Assigned(rec) then EnsureFullGate(rec);

              // Restore MSWP swap data to build format (before BUILD)
              // Priority order:
              //   1) Exact per-record MSWP override (Type=MSWP, Index=TTNNSSOO) always wins
              //   2) If build.csv provides Base/Dest pairs, use them
              //   3) If build.csv has no pairs, use label.csv overrides (Opt/Slot/ObjName/ObjType)
              //   4) Fall back to built-in default path

              pairsCSV := ResolveExactMSWPPairsCSV(objTypeIdx, objNameIdx, slotIdx, optIdx);
              if Trim(pairsCSV) <> '' then begin
                mswpPairsBase.Clear;
                mswpPairsDest.Clear;
                ParsePairListsFromCSVString(pairsCSV, mswpPairsBase, mswpPairsDest);
                ExpandBGSMListTokens(mswpPairsBase, objTypeTokenBGSM, objNameToken, slotStr, optStr);
                ExpandBGSMListTokens(mswpPairsDest, objTypeTokenBGSM, objNameToken, slotStr, optStr);

                if mswpPairsBase.Count > 0 then begin
                  if hasCRI then begin
                    if gDoBuildMSWP then EnsureAndRebuildMSWP(gTargetFile, mswpNew, mswpPairsBase, mswpPairsDest, False, objTypeTokenBGSM, objNameToken, slotStr, '');
                  end else begin
                    if gDoBuildMSWP then EnsureAndRebuildMSWP(gTargetFile, mswpNew, mswpPairsBase, mswpPairsDest, False, objTypeTokenBGSM, objNameToken, slotStr, '');
                  end;
                end else begin
                  // empty override -> ignore
                  pairsCSV := '';
                end;
              end;

              if Trim(pairsCSV) = '' then begin
                if useDefaultPairs then begin
                  // Try broader label.csv overrides (Opt/Slot/ObjName/ObjType)
                  pairsCSV := ResolvePairsCSVForMSWP(objTypeIdx, objNameIdx, slotIdx, optIdx, numObjType);
                  mswpPairsBase.Clear;
                  mswpPairsDest.Clear;
                  if Trim(pairsCSV) <> '' then begin
                    ParsePairListsFromCSVString(pairsCSV, mswpPairsBase, mswpPairsDest);
                    ExpandBGSMListTokens(mswpPairsBase, objTypeTokenBGSM, objNameToken, slotStr, optStr);
                    ExpandBGSMListTokens(mswpPairsDest, objTypeTokenBGSM, objNameToken, slotStr, optStr);
                  end;

                  if mswpPairsBase.Count > 0 then begin
                    if hasCRI then begin
                      if gDoBuildMSWP then EnsureAndRebuildMSWP(gTargetFile, mswpNew, mswpPairsBase, mswpPairsDest, False, objTypeTokenBGSM, objNameToken, slotStr, '');
                    end else begin
                      if gDoBuildMSWP then EnsureAndRebuildMSWP(gTargetFile, mswpNew, mswpPairsBase, mswpPairsDest, False, objTypeTokenBGSM, objNameToken, slotStr, '');
                    end;
                  end else begin
                    // built-in default path
                    if hasCRI then begin
                      if gDoBuildMSWP then EnsureAndRebuildMSWP(gTargetFile, mswpNew, baseList, destList, True, objTypeTokenBGSM, objNameToken, slotStr, '');
                    end else begin
                      if gDoBuildMSWP then EnsureAndRebuildMSWP(gTargetFile, mswpNew, baseList, destList, True, objTypeTokenBGSM, objNameToken, slotStr, '');
                    end;
                  end;
                end else begin
                  // Explicit pairs from build.csv
                  if hasCRI then begin
                    if gDoBuildMSWP then EnsureAndRebuildMSWP(gTargetFile, mswpNew, baseList, destList, False, objTypeTokenBGSM, objNameToken, slotStr, '');
                  end else begin
                    if gDoBuildMSWP then EnsureAndRebuildMSWP(gTargetFile, mswpNew, baseList, destList, False, objTypeTokenBGSM, objNameToken, slotStr, '');
                  end;
                end;
              end;
            end;
          end;
        end;
      end;
    end;
  finally
    mswpPairsDest.Free;
    mswpPairsBase.Free;
    destList.Free;
    baseList.Free;
    slRow.Free;
  end;
end;

// @anchor RUN_BuildPass
procedure BuildPass(buildLines: TStringList);
var
  i: integer;
  line, projectId: string;
  objTypeStart, numObjType, objNameStart, numObjName: integer;
  slotStart, numSlot, optStart, numOpt: integer;
    effNumOpt: integer;
  hasOptDim: boolean;
criStartStr, criStepStr: string;
  hasCRI: boolean;
  criStart, criStep, criVal: double;
  useDefaultPairs: boolean;
  pairsCSV: string;
  objTypeOff, objNameOff, slotOff, optOff: integer;
  objTypeIdx, objNameIdx, slotIdx, optIdx: integer;
  objTypeToken, objTypeTokenBGSM, objNameToken: string;
  canonicalSeg: string;
  legacySeg: string;
  slotStr, optStr: string;

  maEdid, apEdid: string;
  mswpEdid, omodEdid, cobjEdid, miscEdid: string;
  variantEdid: string;
  templateEdid: string;
  objNameTok, optEdidTok, varTok: string;
  templateRec, variantRec: IInterface;

  legacyMSWPEdid, legacyOMODEdid, legacyCOBJEdid, legacyMISCEdid: string;
  maRec, apRec, mswpRec, omodRec: IInterface;
  formType: string;
  isMSWPOnly: boolean;
begin
  if Assigned(gBuildSlRow) then gBuildSlRow.Free;
  gBuildSlRow := TStringList.Create;
  if Assigned(gBuildBaseList) then gBuildBaseList.Free;
  gBuildBaseList := TStringList.Create;
  if Assigned(gBuildDestList) then gBuildDestList.Free;
  gBuildDestList := TStringList.Create;
  gMSWPPairsBase := TStringList.Create;
  gMSWPPairsDest := TStringList.Create;
  try
    gBuildSlRow.StrictDelimiter := True;
    gBuildSlRow.Delimiter := ',';

    // MSWP-only: no OMOD/COBJ/MISC; skip KYWD generation.
    isMSWPOnly := gDoBuildMSWP and (not gDoBuildOMOD) and (not gDoBuildCOBJ) and (not gDoBuildMISC);

    for i := 0 to buildLines.Count - 1 do begin
      line := Trim(buildLines[i]);
      if line = '' then Continue;

      // Allow comment lines in build.csv
      if (Copy(line, 1, 1) = '#') or (Copy(line, 1, 1) = ';') then Continue;

      gBuildSlRow.Clear;
      gBuildSlRow.DelimitedText := buildLines[i];
      if IsBuildHeaderRow(gBuildSlRow) then Continue;
      if gBuildSlRow.Count < 11 then begin
        AddMessage('Skipping build row ' + IntToStr(i+1) + ': not enough columns');
        Continue;
      end;

      projectId := TrimOrEmpty(GetCol(gBuildSlRow, 0));
      objTypeStart := SafeInt(GetCol(gBuildSlRow, 1), -1);
      numObjType   := SafeInt(GetCol(gBuildSlRow, 2), 0);
      objNameStart := SafeInt(GetCol(gBuildSlRow, 3), -1);
      numObjName   := SafeInt(GetCol(gBuildSlRow, 4), 0);
      slotStart    := SafeInt(GetCol(gBuildSlRow, 5), -1);
      numSlot      := SafeInt(GetCol(gBuildSlRow, 6), 0);
      optStart     := SafeInt(GetCol(gBuildSlRow, 7), -1);
      numOpt       := SafeInt(GetCol(gBuildSlRow, 8), 0);

      criStartStr  := TrimOrEmpty(GetCol(gBuildSlRow, 9));
      criStepStr   := TrimOrEmpty(GetCol(gBuildSlRow, 10));
      hasCRI := criStartStr <> '';
      criStart := SafeFloat(criStartStr, 0.0);
      criStep  := SafeFloat(criStepStr, 0.0);

      if (projectId = '') or (numObjType <= 0) or (numObjName <= 0) or (numSlot <= 0) or (numOpt < 0) then begin
        AddMessage('Skipping build row ' + IntToStr(i+1) + ': invalid ProjectID or counts');
        Continue;
      end;

      // Opt dimension can be disabled (NumOpt = 0). In that case we run the opt loop once,
      // omit any _optNN suffix from EDIDs, and ignore optStart.
      hasOptDim := (numOpt > 0);
      effNumOpt := numOpt;
      if effNumOpt <= 0 then effNumOpt := 1;
      if not hasOptDim then optStart := 0;

      ParsePairLists(gBuildSlRow, 11, gBuildBaseList, gBuildDestList);
      useDefaultPairs := gBuildBaseList.Count = 0;

      for objTypeOff := 0 to numObjType - 1 do begin
        objTypeIdx := objTypeStart + objTypeOff;
        if (objTypeIdx < 0) or (objTypeIdx > 99) then Continue;
        objTypeToken := GetMapVal('ObjType', objTypeIdx);
        if Trim(objTypeToken) = '' then begin
          AddMessage('ERROR: Missing ObjType token for index ' + Pad2(objTypeIdx) + ' (row ' + IntToStr(i+1) + ')');
          Continue;
        end;

        // EDID uses alias mapping (e.g. clothes -> armor). BGSM defaults use raw token.
        objTypeTokenBGSM := Trim(objTypeToken);
        objTypeToken := ObjTypeTokenForEDID(objTypeToken);

        formType := CanonicalFormTypeFromObjTypeToken(objTypeToken);

        for objNameOff := 0 to numObjName - 1 do begin
          objNameIdx := objNameStart + objNameOff;
          if (objNameIdx < 0) or (objNameIdx > 99) then Continue;
          objNameToken := GetObjNameToken(objTypeIdx, objNameIdx, numObjType);
          if Trim(objNameToken) = '' then begin
            if numObjType > 1 then
              AddMessage('ERROR: Missing ObjName token for ObjType ' + Pad2(objTypeIdx) + ' and ObjName ' + Pad2(objNameIdx) +
                         ' (expect label.csv: ObjName,' + Pad2(objTypeIdx) + Pad2(objNameIdx) + ',<token>) (row ' + IntToStr(i+1) + ')')
            else
              AddMessage('ERROR: Missing ObjName token for index ' + Pad2(objNameIdx) + ' (row ' + IntToStr(i+1) + ')');
            Continue;
          end;

          canonicalSeg := CanonicalObjSeg(objTypeIdx, objNameIdx, numObjType);
          legacySeg := 'obj' + Pad2(objNameIdx);

          // MA/AP (KYWD) only required when building OMOD/MISC/COBJ pipelines.
          // In MSWP-only mode, skip keyword generation entirely.
          if not isMSWPOnly then begin
            // MA: create if missing
            maEdid := kywdMAPrefix + projectId + '_' + canonicalSeg;
            if (numObjType > 1) and (objTypeOff = 0) then
              TryMigrateRecordByEdid(gTargetFile, 'KYWD', kywdMAPrefix + projectId + '_' + legacySeg, maEdid);
            maRec := EnsureKYWDInTarget(gTargetFile, maEdid, 'Mod Association');
          end else begin
            maRec := nil;
          end;

          for slotOff := 0 to numSlot - 1 do begin
            slotIdx := slotStart + slotOff;
            if (slotIdx < 0) or (slotIdx > 99) then Continue;
            slotStr := Pad2(slotIdx);

            if not isMSWPOnly then begin
              apEdid := kywdAPPrefix + projectId + '_' + canonicalSeg + '_slot' + slotStr;
              if (numObjType > 1) and (objTypeOff = 0) then
                TryMigrateRecordByEdid(gTargetFile, 'KYWD', kywdAPPrefix + projectId + '_' + legacySeg + '_slot' + slotStr, apEdid);
              apRec := EnsureKYWDInTarget(gTargetFile, apEdid, 'Attach Point');
            end else begin
              apRec := nil;
            end;

            for optOff := 0 to effNumOpt - 1 do begin
              optIdx := optStart + optOff;
              if hasOptDim then begin
                if (optIdx < 0) or (optIdx > 99) then Continue;
                optStr := Pad2(optIdx);
              end else begin
                // No opt dimension: normalize.
                optIdx := 0;
                optStr := '00';
              end;

              mswpRec := nil;

              if hasCRI then
                criVal := criStart + (((slotIdx - slotStart) * effNumOpt + (optIdx - optStart)) * criStep)
              else
                criVal := 0.0;

              // MSWP-only mode uses OPT text (not OPT index) in EditorIDs and creates physical item variants.
              if gDoBuildMSWP and (not gDoBuildOMOD) and (not gDoBuildCOBJ) and (not gDoBuildMISC) then begin
                objNameTok := GetObjNameToken(objTypeIdx, objNameIdx, numObjType);

                // When NumOpt = 0 (no opt dimension), use the SLOT label as the variant token
                // so EDIDs read like TruckOrange / LargeCrateYellow instead of Truckvar00.
                if not hasOptDim then begin
                  varTok := FreeTextTokenForEDID(SlotLabelMap[slotIdx]);
                  if varTok = '' then varTok := 'var' + slotStr; // fallback
                end else begin
                  varTok := 'var' + slotStr;
                end;

                // If NumOpt = 0, we intentionally omit any opt token/suffix from EDIDs.
                if hasOptDim then begin
                  optEdidTok := FreeTextTokenForEDID(OptLabelMap[optIdx]);
                  if optEdidTok = '' then optEdidTok := 'opt' + optStr;
                  variantEdid := projectId + '_' + objNameTok + varTok + '_' + optEdidTok;
                  mswpEdid := mswpPrefix + objTypeToken + '_' + projectId + '_' + objNameTok + varTok + '_' + optEdidTok;
                end else begin
                  optEdidTok := '';
                  variantEdid := projectId + '_' + objNameTok + varTok;
                  mswpEdid := mswpPrefix + objTypeToken + '_' + projectId + '_' + objNameTok + varTok;
                end;
              end else begin
                mswpEdid := mswpPrefix + objTypeToken + '_' + projectId + '_' + canonicalSeg
                  + '_slot' + slotStr + OptSuffix(hasOptDim, optStr);
              end;
              legacyMSWPEdid := mswpPrefix + objTypeToken + '_' + projectId + '_' + legacySeg + '_slot' + slotStr + OptSuffix(hasOptDim, optStr);
              if (numObjType > 1) and (objTypeOff = 0) then
                TryMigrateRecordByEdid(gTargetFile, 'MSWP', legacyMSWPEdid, mswpEdid);

              // MSWP always built
              // (1) Exact per-record override (label.csv: Type=MSWP, Index=TTNNSSOO)
              pairsCSV := ResolveExactMSWPPairsCSV(objTypeIdx, objNameIdx, slotIdx, optIdx);
              gMSWPPairsBase.Clear;
              gMSWPPairsDest.Clear;
              if Trim(pairsCSV) <> '' then begin
                ParsePairListsFromCSVString(pairsCSV, gMSWPPairsBase, gMSWPPairsDest);
                ExpandBGSMListTokens(gMSWPPairsBase, objTypeTokenBGSM, objNameToken, slotStr, optStr);
                ExpandBGSMListTokens(gMSWPPairsDest, objTypeTokenBGSM, objNameToken, slotStr, optStr);
              end;

              if gMSWPPairsBase.Count > 0 then begin
                if hasCRI then begin
                  if gDoBuildMSWP then mswpRec := EnsureAndRebuildMSWP(gTargetFile, mswpEdid, gMSWPPairsBase, gMSWPPairsDest, False, objTypeTokenBGSM, objNameToken, slotStr, '');
                end else begin
                  if gDoBuildMSWP then mswpRec := EnsureAndRebuildMSWP(gTargetFile, mswpEdid, gMSWPPairsBase, gMSWPPairsDest, False, objTypeTokenBGSM, objNameToken, slotStr, '');
                end;
              end else if useDefaultPairs then begin
                // (2) Default path OR scoped override (Opt -> Slot -> ObjName -> ObjType)
                pairsCSV := ResolvePairsCSVForMSWP(objTypeIdx, objNameIdx, slotIdx, optIdx, numObjType);
                gMSWPPairsBase.Clear;
                gMSWPPairsDest.Clear;
                if Trim(pairsCSV) <> '' then begin
                  ParsePairListsFromCSVString(pairsCSV, gMSWPPairsBase, gMSWPPairsDest);
                  ExpandBGSMListTokens(gMSWPPairsBase, objTypeTokenBGSM, objNameToken, slotStr, optStr);
                  ExpandBGSMListTokens(gMSWPPairsDest, objTypeTokenBGSM, objNameToken, slotStr, optStr);
                end;

                if gMSWPPairsBase.Count > 0 then begin
                  if hasCRI then begin
                    if gDoBuildMSWP then mswpRec := EnsureAndRebuildMSWP(gTargetFile, mswpEdid, gMSWPPairsBase, gMSWPPairsDest, False, objTypeTokenBGSM, objNameToken, slotStr, '');
                  end else begin
                    if gDoBuildMSWP then mswpRec := EnsureAndRebuildMSWP(gTargetFile, mswpEdid, gMSWPPairsBase, gMSWPPairsDest, False, objTypeTokenBGSM, objNameToken, slotStr, '');
                  end;
                end else begin
                  // built-in fallback: 1 swap pair derived from objTypeTokenBGSM + objNameToken + slotStr
                  if hasCRI then begin
                    if gDoBuildMSWP then mswpRec := EnsureAndRebuildMSWP(gTargetFile, mswpEdid, gBuildBaseList, gBuildDestList, True, objTypeTokenBGSM, objNameToken, slotStr, '');
                  end else begin
                    if gDoBuildMSWP then mswpRec := EnsureAndRebuildMSWP(gTargetFile, mswpEdid, gBuildBaseList, gBuildDestList, True, objTypeTokenBGSM, objNameToken, slotStr, '');
                  end;
                end;
              end else begin
                // (3) Explicit pairs from build.csv
                if hasCRI then begin
                  if gDoBuildMSWP then mswpRec := EnsureAndRebuildMSWP(gTargetFile, mswpEdid, gBuildBaseList, gBuildDestList, False, objTypeTokenBGSM, objNameToken, slotStr, '');
                end else begin
                  if gDoBuildMSWP then mswpRec := EnsureAndRebuildMSWP(gTargetFile, mswpEdid, gBuildBaseList, gBuildDestList, False, objTypeTokenBGSM, objNameToken, slotStr, '');
                end;
              end;

              if gDoBuildMSWP and (not Assigned(mswpRec)) then begin
                // Allow CRI-only MSWP-only runs (no swap pairs + CRIStart set)
                if (not hasCRI) or (gBuildBaseList.Count > 0) or useDefaultPairs then begin
                  AddMessage('ERROR: MSWP build failed: ' + mswpEdid);
                  Continue;
                end;
              end;
// MSWP-only: duplicate a template record and attach this MSWP to the model.
              if gDoBuildMSWP and (not gDoBuildOMOD) and (not gDoBuildCOBJ) and (not gDoBuildMISC) then begin
                templateEdid := GetObjNameTemplateEdid(objTypeIdx, objNameIdx, numObjType);
                if Trim(templateEdid) = '' then begin
                  AddMessage('ERROR: Missing TemplateEdid for OBJNAME ' + Pad2(objTypeIdx) + Pad2(objNameIdx) + ' (Type,Index,Text,TemplateEdid,Base1,Dest1,...)');
                  Continue;
                end;

                templateRec := FindRecordByEdidInAnySig(templateEdid);
                if not Assigned(templateRec) then begin
                  AddMessage('ERROR: Template record not found: ' + templateEdid);
                  Continue;
                end;

                variantRec := FindRecordInTargetByEdid(gTargetFile, Signature(templateRec), variantEdid);
                if not Assigned(variantRec) then begin
                  variantRec := wbCopyElementToFile(templateRec, gTargetFile, True, True);
                  if not Assigned(variantRec) then begin
                    AddMessage('ERROR: Failed to copy template for variant: ' + variantEdid);
                    Continue;
                  end;
                  SetElementEditValues(variantRec, 'EDID', variantEdid);
                end;

                if Assigned(mswpRec) then SetModelMSWP(variantRec, mswpRec);
                if hasCRI then SetModelCRI(variantRec, criVal);
              end;

// Compute canonical derived EDIDs for optional record types
              omodEdid := omodPrefix + objTypeToken + '_' + projectId + '_' + canonicalSeg + '_slot' + slotStr + OptSuffix(hasOptDim, optStr);
              miscEdid := miscPrefix + omodEdid;
              legacyOMODEdid := omodPrefix + objTypeToken + '_' + projectId + '_' + legacySeg + '_slot' + slotStr + OptSuffix(hasOptDim, optStr);
              legacyMISCEdid := miscPrefix + legacyOMODEdid;

              if (numObjType > 1) and (objTypeOff = 0) then begin
                if gDoBuildOMOD then
                  TryMigrateRecordByEdid(gTargetFile, 'OMOD', legacyOMODEdid, omodEdid);
                if gDoBuildMISC then
                  TryMigrateRecordByEdid(gTargetFile, 'MISC', legacyMISCEdid, miscEdid);
              end;

              // OMOD (optional)
              omodRec := nil;
              if gDoBuildOMOD then
                omodRec := EnsureAndRebuildOMOD(gTargetFile, omodEdid, formType, apRec, maRec, mswpRec, gDoBuildMISC, miscEdid)
              else begin
                // "COBJ-only" / "MISC-only" maintenance runs: reuse existing OMOD by EDID
                omodRec := FindRecordInTargetByEdid(gTargetFile, 'OMOD', omodEdid);
                if not Assigned(omodRec) then
                  omodRec := FindRecordByEditorIDInFiles('OMOD', omodEdid);
              end;

              // MISC (optional, independent)
              if gDoBuildMISC then begin
                // Build the loose mod MISC even if OMOD isn't being rebuilt.
                EnsureLooseModMISC(gTargetFile, miscEdid);
              end;

              // COBJ (optional, independent of OMOD rebuild but still requires the created object to exist)
              if gDoBuildCOBJ then begin
                if not Assigned(omodRec) then begin
                  AddMessage('WARNING: Skipping COBJ because created OMOD was not found: ' + omodEdid);
                end else begin
                  cobjEdid := cobjPrefix + omodEdid;
                  legacyCOBJEdid := cobjPrefix + legacyOMODEdid;
                  if (numObjType > 1) and (objTypeOff = 0) then
                    TryMigrateRecordByEdid(gTargetFile, 'COBJ', legacyCOBJEdid, cobjEdid);
                  EnsureAndRebuildCOBJ(gTargetFile, cobjEdid, omodRec, ResolveCobjRecipe(objTypeIdx, objNameIdx, slotIdx, optIdx));
                end;
              end;
            end;
          end;
        end;
      end;
    end;
  finally
    if Assigned(gMSWPPairsDest) then begin
      gMSWPPairsDest.Free;
      gMSWPPairsDest := nil;
    end;
    if Assigned(gMSWPPairsBase) then begin
      gMSWPPairsBase.Free;
      gMSWPPairsBase := nil;
    end;
    if Assigned(gBuildDestList) then begin
      gBuildDestList.Free;
      gBuildDestList := nil;
    end;
    if Assigned(gBuildBaseList) then begin
      gBuildBaseList.Free;
      gBuildBaseList := nil;
    end;
    if Assigned(gBuildSlRow) then begin
      gBuildSlRow.Free;
      gBuildSlRow := nil;
    end;
  end;
end;

// @anchor RUN_LabelPass
procedure LabelPass(buildLines: TStringList);
var
  i: integer;
  slRow: TStringList;
  line, projectId: string;
  objTypeStart, numObjType, objNameStart, numObjName: integer;
  slotStart, numSlot, optStart, numOpt: integer;

    effNumOpt: integer;
  hasOptDim: boolean;
objTypeOff, objNameOff, slotOff, optOff: integer;
  objTypeIdx, objNameIdx, slotIdx, optIdx: integer;
  objTypeToken, objNameToken: string;
  canonicalSeg, labeledSeg: string;
  slotStr, optStr: string;

  slotLabel, optLabel: string;

  maOld, maNew, apOld, apNew: string;
  mswpOld, mswpNew: string;
  omodOld, omodNew: string;
  cobjOld, cobjNew: string;
  miscOld, miscNew: string;

  rec: IInterface;
  fullText: string;
  isMSWPOnly: boolean;
begin
  slRow := TStringList.Create;
  try
    slRow.StrictDelimiter := True;
    slRow.Delimiter := ',';

    isMSWPOnly := gDoBuildMSWP and (not gDoBuildOMOD) and (not gDoBuildCOBJ) and (not gDoBuildMISC);

    for i := 0 to buildLines.Count - 1 do begin
      line := Trim(buildLines[i]);
      if line = '' then Continue;

      // Allow comment lines in build.csv
      if (Copy(line, 1, 1) = '#') or (Copy(line, 1, 1) = ';') then Continue;

      slRow.Clear;
      slRow.DelimitedText := buildLines[i];
      if IsBuildHeaderRow(slRow) then Continue;
      if slRow.Count < 9 then begin
        AddMessage('Skipping build row ' + IntToStr(i+1) + ': not enough columns');
        Continue;
      end;

      projectId := TrimOrEmpty(GetCol(slRow, 0));
      objTypeStart := SafeInt(GetCol(slRow, 1), -1);
      numObjType   := SafeInt(GetCol(slRow, 2), 0);
      objNameStart := SafeInt(GetCol(slRow, 3), -1);
      numObjName   := SafeInt(GetCol(slRow, 4), 0);
      slotStart    := SafeInt(GetCol(slRow, 5), -1);
      numSlot      := SafeInt(GetCol(slRow, 6), 0);
      optStart     := SafeInt(GetCol(slRow, 7), -1);
      numOpt       := SafeInt(GetCol(slRow, 8), 0);

      if (projectId = '') or (numObjType <= 0) or (numObjName <= 0) or (numSlot <= 0) or (numOpt < 0) then begin
        AddMessage('Skipping build row ' + IntToStr(i+1) + ': invalid ProjectID or counts');
        Continue;
      end;

      // Opt dimension can be disabled (NumOpt = 0). In that case we run the opt loop once,
      // omit any _optNN suffix from EDIDs, and ignore optStart.
      hasOptDim := (numOpt > 0);
      effNumOpt := numOpt;
      if effNumOpt <= 0 then effNumOpt := 1;
      if not hasOptDim then optStart := 0;

      for objTypeOff := 0 to numObjType - 1 do begin
        objTypeIdx := objTypeStart + objTypeOff;
        if (objTypeIdx < 0) or (objTypeIdx > 99) then Continue;
        objTypeToken := GetMapVal('ObjType', objTypeIdx);
        if Trim(objTypeToken) = '' then Continue;
        objTypeToken := ObjTypeTokenForEDID(objTypeToken);

        for objNameOff := 0 to numObjName - 1 do begin
          objNameIdx := objNameStart + objNameOff;
          if (objNameIdx < 0) or (objNameIdx > 99) then Continue;
          objNameToken := GetObjNameToken(objTypeIdx, objNameIdx, numObjType);
          if Trim(objNameToken) = '' then begin
            if numObjType > 1 then
              AddMessage('ERROR: Missing ObjName token for ObjType ' + Pad2(objTypeIdx) + ' and ObjName ' + Pad2(objNameIdx) +
                         ' (expect label.csv: ObjName,' + Pad2(objTypeIdx) + Pad2(objNameIdx) + ',<token>) (row ' + IntToStr(i+1) + ')')
            else
              AddMessage('ERROR: Missing ObjName token for index ' + Pad2(objNameIdx) + ' (row ' + IntToStr(i+1) + ')');
            Continue;
          end;

          canonicalSeg := CanonicalObjSeg(objTypeIdx, objNameIdx, numObjType);
          labeledSeg := objNameToken;

          if not isMSWPOnly then begin
            // MA rename + FULL labeling
            maOld := kywdMAPrefix + projectId + '_' + canonicalSeg;
            maNew := kywdMAPrefix + projectId + '_' + labeledSeg;
            rec := TryRenameRecord(gTargetFile, 'KYWD', maOld, maNew);
            if Assigned(rec) then
              SetFullIfGated(rec, labeledSeg);
          end;

          for slotOff := 0 to numSlot - 1 do begin
            slotIdx := slotStart + slotOff;
            if (slotIdx < 0) or (slotIdx > 99) then Continue;
            slotStr := Pad2(slotIdx);
            slotLabel := GetMapVal('Slot', slotIdx);

            if not isMSWPOnly then begin
              // AP rename + FULL labeling
              apOld := kywdAPPrefix + projectId + '_' + canonicalSeg + '_slot' + slotStr;
              apNew := kywdAPPrefix + projectId + '_' + labeledSeg + '_slot' + slotStr;
              rec := TryRenameRecord(gTargetFile, 'KYWD', apOld, apNew);
              if Assigned(rec) and (slotLabel <> '') then
                SetFullIfGated(rec, NormalizeSlotLabelForAP(slotLabel));
            end;

            for optOff := 0 to effNumOpt - 1 do begin
              optIdx := optStart + optOff;
              if hasOptDim then begin
                if (optIdx < 0) or (optIdx > 99) then Continue;
                optStr := Pad2(optIdx);
              end else begin
                // No opt dimension: normalize.
                optIdx := 0;
                optStr := '00';
              end;
optLabel := GetMapVal('Opt', optIdx);

              // MSWP rename
              mswpOld := mswpPrefix + objTypeToken + '_' + projectId + '_' + canonicalSeg + '_slot' + slotStr + OptSuffix(hasOptDim, optStr);
              mswpNew := mswpPrefix + objTypeToken + '_' + projectId + '_' + labeledSeg + '_slot' + slotStr + OptSuffix(hasOptDim, optStr);
              TryRenameRecord(gTargetFile, 'MSWP', mswpOld, mswpNew);

              // OMOD rename + FULL labeling
              omodOld := omodPrefix + objTypeToken + '_' + projectId + '_' + canonicalSeg + '_slot' + slotStr + OptSuffix(hasOptDim, optStr);
              omodNew := omodPrefix + objTypeToken + '_' + projectId + '_' + labeledSeg + '_slot' + slotStr + OptSuffix(hasOptDim, optStr);
              rec := TryRenameRecord(gTargetFile, 'OMOD', omodOld, omodNew);
              if Assigned(rec) and (slotLabel <> '') and (optLabel <> '') then begin
                fullText := slotLabel + optLabel;
                SetFullIfGated(rec, fullText);
              end;

              // COBJ rename (based on OMOD)
              cobjOld := cobjPrefix + omodOld;
              cobjNew := cobjPrefix + omodNew;
              TryRenameRecord(gTargetFile, 'COBJ', cobjOld, cobjNew);

              // MISC rename + FULL labeling
              miscOld := miscPrefix + omodOld;
              miscNew := miscPrefix + omodNew;
              rec := TryRenameRecord(gTargetFile, 'MISC', miscOld, miscNew);
              if Assigned(rec) and (labeledseg <> '') and (slotLabel <> '') and (optLabel <> '') then begin
                fullText := labeledseg + ' - ' + slotLabel + optLabel;
                SetFullIfGated(rec, fullText);
              end;
            end;
          end;
        end;
      end;
    end;
  finally
    slRow.Free;
  end;
end;

// ============================================================================
// [UI] UI scaling helpers
// ============================================================================

function UI_S(const v: integer): integer;
begin
  if gUITextScalePct <= 0 then gUITextScalePct := 100;
  Result := (v * gUITextScalePct + 50) div 100;
  if Result < 1 then Result := 1;
end;

procedure UI_ApplyFormFont(frm: TForm);
var
  fs: integer;
begin
  if not Assigned(frm) then Exit;
  if gUITextScalePct <= 0 then gUITextScalePct := 100;
  fs := frm.Font.Size;
  fs := (fs * gUITextScalePct + 50) div 100;
  if fs < 6 then fs := 6;
  frm.Font.Size := fs;
end;

// ============================================================================
// [UI] UI: Splash image (click anywhere to continue)
// ============================================================================

var
  gSplashForm: TForm;

procedure UI_SplashClick(Sender: TObject);
begin
  if Assigned(gSplashForm) then
    gSplashForm.ModalResult := mrOk;
end;

// Shows a borderless splash window. Clicking anywhere closes it.
procedure UI_ShowSplashClickAny(const imagePath: string);
var
  frm: TForm;
  img: TImage;
begin
  frm := TForm.Create(nil);
  try
    gSplashForm := frm;

    frm.Position := poScreenCenter;
    frm.BorderStyle := bsNone;
    frm.FormStyle := fsStayOnTop;
    frm.Color := clBlack;

    // Fixed size; image is stretched proportionally to fit.
    frm.ClientWidth := 640;
    frm.ClientHeight := 640;

    frm.OnClick := UI_SplashClick;

    img := TImage.Create(frm);
    img.Parent := frm;
    img.Align := alClient;
    img.Stretch := True;
    img.Proportional := True;
    img.Center := True;
    img.OnClick := UI_SplashClick;

    // If it loads, great; otherwise you still get a click-through splash.
    try
      img.Picture.LoadFromFile(imagePath);
    except
      // ignore load errors
    end;

    frm.ShowModal;
  finally
    gSplashForm := nil;
    frm.Free;
  end;
end;

// Shows up to two startup splash screens if the PNGs exist.
// Priority:
//   (1) splash1.png (fallback: splash.png)
//   (2) splash2.png
procedure UI_ShowStartupSplashes;
var
  p1, p2: string;
begin
  p1 := ProgramPath + 'Edit Scripts\FO4_CSV_Generator_Labeler\splash1.png';
  if not FileExists(p1) then
    p1 := ProgramPath + 'Edit Scripts\FO4_CSV_Generator_Labeler\splash.png';

  p2 := ProgramPath + 'Edit Scripts\FO4_CSV_Generator_Labeler\splash2.png';

  if FileExists(p1) then UI_ShowSplashClickAny(p1);
  if FileExists(p2) then UI_ShowSplashClickAny(p2);
end;


// ============================================================================
// [UI] UI: Embedded Text-Size (Zoom) selector panel + "reopen" behavior
// ============================================================================

const
  UI_SCALE_PANEL_H   = 28;  // base (unscaled) height reserved for compact zoom row
  UI_SCALE_PANEL_GAP = 6;   // base (unscaled) gap below zoom row
  MR_REOPEN          = 1001; // custom ModalResult used to force dialog rebuild after zoom change

var
  gUIScaleOwnerForm: TForm;
function UI_ScalePctToIndex(pct: integer): integer;
begin
  if pct <= 110 then Result := 0
  else if pct <= 135 then Result := 1
  else if pct <= 160 then Result := 2
  else if pct <= 185 then Result := 3
  else Result := 4;
end;

function UI_ScaleIndexToPct(idx: integer): integer;
begin
  case idx of
    0: Result := 100;
    1: Result := 125;
    2: Result := 150;
    3: Result := 175;
    4: Result := 200;
  else
    Result := 100;
  end;
end;

// When user changes zoom inside a dialog, close it and let the caller reopen at new scale.
procedure UI_ScalePanel_Change(Sender: TObject);
var
  cb: TComboBox;
  newPct: integer;
begin
  // Sender is the combo box itself.
  if not Assigned(Sender) then Exit;
  cb := TComboBox(Sender);
  if not Assigned(cb) then Exit;

  newPct := UI_ScaleIndexToPct(cb.ItemIndex);

  // If the zoom changes, close the current dialog with a special result so the caller can recreate it.
  if newPct <> gUITextScalePct then begin
    gUITextScalePct := newPct;
    if Assigned(gUIScaleOwnerForm) then
      gUIScaleOwnerForm.ModalResult := MR_REOPEN;
  end;
end;

// Adds a compact zoom selector in the top-right of frm, and advances yTop below it.
procedure UI_AddScalePanel(frm: TForm; var yTop: integer);
var
  lbl: TLabel;
  cb: TComboBox;
  pad, gap: integer;
begin
  if not Assigned(frm) then Exit;

  gUIScaleOwnerForm := frm;

  pad := UI_S(12);
  gap := UI_S(UI_SCALE_PANEL_GAP);

  cb := TComboBox.Create(frm);
  cb.Parent := frm;
  cb.Style := csDropDownList;
  cb.Width := UI_S(92);
  cb.Top := yTop;
  cb.Left := frm.ClientWidth - pad - cb.Width;
  cb.Anchors := [akTop, akRight];
  cb.Items.Add('100%');
  cb.Items.Add('125%');
  cb.Items.Add('150%');
  cb.Items.Add('175%');
  cb.Items.Add('200%');
  cb.ParentFont := True;

  // Set selection BEFORE wiring events (prevents reopen during construction).
  cb.ItemIndex := UI_ScalePctToIndex(gUITextScalePct);
  cb.OnChange := UI_ScalePanel_Change;
  cb.OnClick  := UI_ScalePanel_Change;

  lbl := TLabel.Create(frm);
  lbl.Parent := frm;
  lbl.Caption := 'Zoom:';
  lbl.ParentFont := True;
  lbl.AutoSize := True;
  lbl.Top := yTop + UI_S(4);
  lbl.Left := cb.Left - lbl.Width - UI_S(6);
  lbl.Anchors := [akTop, akRight];

  // Advance the caller's top cursor below the compact zoom row.
  yTop := cb.Top + cb.Height + gap;
end;

procedure UI_InitDialogForm(frm: TForm; baseW, baseH: integer; const captionText: string; var yTop: integer);
begin
  if not Assigned(frm) then Exit;

  frm.ClientWidth := UI_S(baseW);
  frm.ClientHeight := UI_S(baseH + UI_SCALE_PANEL_H + UI_SCALE_PANEL_GAP);
  frm.Position := poScreenCenter;
  frm.Caption := captionText;
  UI_ApplyFormFont(frm);

  yTop := UI_S(8);
  UI_AddScalePanel(frm, yTop);
end;

// Standard OK/Cancel buttons centered at topY (scaled).
procedure UI_AddOkCancel(frm: TForm; topY: integer; var btnOk, btnCancel: TButton);
begin
  if not Assigned(frm) then Exit;

  btnOk := TButton.Create(frm);
  btnOk.Parent := frm;
  btnOk.Caption := 'OK';
  btnOk.ModalResult := mrOk;

  btnCancel := TButton.Create(frm);
  btnCancel.Parent := frm;
  btnCancel.Caption := 'Cancel';
  btnCancel.ModalResult := mrCancel;

  // Position after creation (button widths depend on current font).
  btnOk.Top := topY;
  btnOk.Left := frm.ClientWidth div 2 - btnOk.Width - UI_S(8);

  btnCancel.Top := topY;
  btnCancel.Left := btnOk.Left + btnOk.Width + UI_S(16);
end;

function FileSelect: IwbFile;
var
  frm: TForm;
  cbFile: TComboBox;
  lblFile: TLabel;
  btnOk, btnCancel: TButton;
  i: integer;
  yTop: integer;
  reopen: boolean;
  modalRes: integer;
begin
  Result := nil;

  reopen := True;

  while reopen do begin
    reopen := False;
    modalRes := mrCancel;

    frm := TForm.Create(nil);
    try
      UI_InitDialogForm(frm, 340, 130, 'Select target file', yTop);

      lblFile := TLabel.Create(frm);
      lblFile.Parent := frm;
      lblFile.Left := UI_S(16);
      lblFile.Top := yTop + UI_S(6);
      lblFile.Caption := 'File:';

      cbFile := TComboBox.Create(frm);
      cbFile.Parent := frm;
      cbFile.Left := UI_S(50);
      cbFile.Top := lblFile.Top - UI_S(2);
      cbFile.Width := frm.ClientWidth - cbFile.Left - UI_S(20);

      cbFile.Items.Clear;
      for i := 0 to FileCount - 1 do
        cbFile.Items.Add(GetFileName(FileByIndex(i)));

      if cbFile.Items.Count > 0 then
        cbFile.ItemIndex := 0;

      UI_AddOkCancel(frm, lblFile.Top + lblFile.Height + UI_S(26), btnOk, btnCancel);

      modalRes := frm.ShowModal();

      if modalRes = MR_REOPEN then begin
        reopen := True;
      end else if modalRes = mrOk then begin
        Result := FileByName(cbFile.Items[cbFile.ItemIndex]);
      end;

    finally
      gUIScaleOwnerForm := nil;
      frm.Free;
    end;

  end;
end;

procedure OptionsDialog_Refresh;
var
  mswpOn, omodOn, cobjOn, miscOn: boolean;
begin
  if not Assigned(gDlgRGMSWP) then Exit;
  if not Assigned(gDlgRGOMOD) then Exit;
  if not Assigned(gDlgRGCOBJ) then Exit;
  if not Assigned(gDlgRGMISC) then Exit;

  // Current requested states
  mswpOn := (gDlgRGMSWP.ItemIndex = 0);
  omodOn := (gDlgRGOMOD.ItemIndex = 0);
  cobjOn := (gDlgRGCOBJ.ItemIndex = 0);
  miscOn := (gDlgRGMISC.ItemIndex = 0);

  // ---- Enforce gate dependencies (state) ----

  // If MISC is on, it requires MSWP+OMOD+COBJ
  if miscOn then begin
    mswpOn := True;
    omodOn := True;
    cobjOn := True;
  end;

  // If OMOD is on, MSWP must be on
  if omodOn then
    mswpOn := True;

  // If MSWP is off, OMOD + MISC must be off
  if not mswpOn then begin
    omodOn := False;
    miscOn := False;
    // (COBJ may remain on -> COBJ-only gate)
  end;

  // MSWP-only gate: MSWP on but OMOD off => force COBJ + MISC off
  if mswpOn and (not omodOn) then begin
    cobjOn := False;
    miscOn := False;
  end;

  // If COBJ is off, MISC must be off
  if not cobjOn then
    miscOn := False;

  // ---- Apply forced states back into UI ----
  if mswpOn then gDlgRGMSWP.ItemIndex := 0 else gDlgRGMSWP.ItemIndex := 1;
  if omodOn then gDlgRGOMOD.ItemIndex := 0 else gDlgRGOMOD.ItemIndex := 1;
  if cobjOn then gDlgRGCOBJ.ItemIndex := 0 else gDlgRGCOBJ.ItemIndex := 1;
  if miscOn then gDlgRGMISC.ItemIndex := 0 else gDlgRGMISC.ItemIndex := 1;

  // ---- Enforce gate dependencies (enabled/disabled controls) ----

  // MSWP can be toggled only if OMOD is off AND MISC is off
  // (because OMOD implies MSWP, and MISC implies both)
  gDlgRGMSWP.Enabled := (not omodOn) and (not miscOn);

  // OMOD can be toggled only if MSWP is on (otherwise meaningless)
  // If MISC is on, lock OMOD (because it is required)
  gDlgRGOMOD.Enabled := mswpOn and (not miscOn);

  // COBJ:
  // - allowed in COBJ-only (MSWP off)
  // - allowed in OMOD gates (OMOD on)
  // - NOT allowed in MSWP-only (MSWP on + OMOD off)
  if mswpOn and (not omodOn) then
    gDlgRGCOBJ.Enabled := False
  else
    gDlgRGCOBJ.Enabled := True;

  // MISC only selectable when MSWP+OMOD+COBJ are all on
  gDlgRGMISC.Enabled := mswpOn and omodOn and cobjOn;
end;

procedure OptionsDialog_MSWPClick(Sender: TObject);
begin
  OptionsDialog_Refresh;
end;

procedure OptionsDialog_OMODClick(Sender: TObject);
begin
  OptionsDialog_Refresh;
end;

procedure OptionsDialog_COBJClick(Sender: TObject);
begin
  OptionsDialog_Refresh;
end;

procedure OptionsDialog_MISCClick(Sender: TObject);
begin
  OptionsDialog_Refresh;
end;

// ============================================================================
// [UI] Build input dialog (manual-first; optional CSV upload link)
// ============================================================================
procedure BuildInput_PairsClick(Sender: TObject);
var
  useExplicit: boolean;
begin
  if not Assigned(gBuildInputPairsRG) then Exit;
  if not Assigned(gBuildInputBaseEdit) then Exit;
  if not Assigned(gBuildInputDestEdit) then Exit;
  if not Assigned(gBuildInputBaseBtn) then Exit;
  if not Assigned(gBuildInputDestBtn) then Exit;

  useExplicit := (gBuildInputPairsRG.ItemIndex = 1);

  gBuildInputBaseEdit.Enabled := useExplicit;
  gBuildInputDestEdit.Enabled := useExplicit;
  gBuildInputBaseBtn.Enabled := useExplicit;
  gBuildInputDestBtn.Enabled := useExplicit;

  if useExplicit then begin
    gBuildInputBaseEdit.Color := clWindow;
    gBuildInputDestEdit.Color := clWindow;
  end else begin
    gBuildInputBaseEdit.Color := clBtnFace;
    gBuildInputDestEdit.Color := clBtnFace;
  end;
end;

procedure BuildInput_UploadClick(Sender: TObject);
begin
  gBuildInputUseCSV := True;
  if Assigned(gBuildInputForm) then
    gBuildInputForm.ModalResult := mrOk;
end;



function NormalizeSelectedBGSMPath(const sIn: string): string;
var
  s, lowerS: string;
  p: integer;
begin
  s := StringReplace(sIn, '/', '\', [rfReplaceAll]);
  lowerS := LowerCase(s);

  // Prefer trimming to relative path under Data\Materials\
  p := Pos('\data\materials\', lowerS);
  if p > 0 then
    Result := Copy(s, p + Length('\data\materials\'), MaxInt)
  else begin
    p := Pos('\materials\', lowerS);
    if p > 0 then
      Result := Copy(s, p + Length('\materials\'), MaxInt)
    else
      Result := s;
  end;

  while (Length(Result) > 0) and (Result[1] = '\') do
    Delete(Result, 1, 1);
end;

procedure BuildInput_BrowseToEdit(ed: TEdit);
var
  dlg: TOpenDialog;
  s: string;
begin
  if not Assigned(ed) then Exit;

  dlg := TOpenDialog.Create(nil);
  try
    dlg.Filter := 'BGSM material (*.bgsm)|*.bgsm|All files (*.*)|*.*';
    dlg.Options := dlg.Options + [ofFileMustExist, ofPathMustExist];
    if dlg.Execute then begin
      s := NormalizeSelectedBGSMPath(dlg.FileName);
      ed.Text := s;
    end;
  finally
    dlg.Free;
  end;
end;

procedure BuildInput_BrowseBaseClick(Sender: TObject);
begin
  BuildInput_BrowseToEdit(gBuildInputBaseEdit);
end;

procedure BuildInput_BrowseDestClick(Sender: TObject);
begin
  BuildInput_BrowseToEdit(gBuildInputDestEdit);
end;

procedure FillComboIntRange(cb: TComboBox; minV, maxV, defaultV: integer);
var
  i, defIdx: integer;
begin
  if not Assigned(cb) then Exit;
  cb.Items.Clear;
  defIdx := -1;
  for i := minV to maxV do begin
    cb.Items.Add(IntToStr(i));
    if i = defaultV then defIdx := cb.Items.Count - 1;
  end;
  if defIdx >= 0 then
    cb.ItemIndex := defIdx
  else if cb.Items.Count > 0 then
    cb.ItemIndex := 0;
end;

function BuildInputDialog(slOut: TStringList): boolean;
var
  frm: TForm;
  lblTitle, lblNote, lblUpload: TLabel;
  gbRow, gbPairs: TGroupBox;
  edProjectID: TEdit;
  cbNumObjType, cbNumObjName, cbNumSlot, cbNumOpt: TComboBox;
  edCRIStart, edCRIStep: TEdit;
  edBase1, edDest1: TEdit;
  btnBrowseBase1, btnBrowseDest1: TButton;
  btnOk, btnCancel: TButton;

  numObjType, numObjName, numSlot, numOpt: integer;
  criStartStr, criStepStr: string;

  header, row: string;
  baseS, destS: string;

  yTop: integer;
  reopen: boolean;
  modalRes: integer;
begin
  Result := False;
  gBuildInputUseCSV := False;

  if not Assigned(slOut) then Exit;

  reopen := True;

  while reopen do begin
    reopen := False;
    modalRes := mrCancel;

    frm := TForm.Create(nil);
    gBuildInputForm := frm;
    try
      UI_InitDialogForm(frm, 640, 550, 'Build Input (single row)', yTop);

      lblTitle := TLabel.Create(frm);
      lblTitle.Parent := frm;
      lblTitle.Left := UI_S(12);
      lblTitle.Top := yTop;
      lblTitle.AutoSize := False;
      lblTitle.WordWrap := True;
      lblTitle.Width := frm.ClientWidth - UI_S(24);
      lblTitle.Height := UI_S(40);
      lblTitle.Caption :=
        'Enter a single build.csv row using constrained selectors. ' +
        'Start values are fixed to 00 for UI entry.';

      lblNote := TLabel.Create(frm);
      lblNote.Parent := frm;
      lblNote.Left := UI_S(12);
      lblNote.Top := lblTitle.Top + lblTitle.Height + UI_S(4);
      lblNote.AutoSize := False;
      lblNote.WordWrap := True;
      lblNote.Width := frm.ClientWidth - UI_S(24);
      lblNote.Height := UI_S(36);
      lblNote.Caption :=
        'For non-00 start values or multi-row inputs, use the upload link below.';

      gbRow := TGroupBox.Create(frm);
      gbRow.Parent := frm;
      gbRow.Left := UI_S(12);
      gbRow.Top := lblNote.Top + lblNote.Height + UI_S(8);
      gbRow.Width := frm.ClientWidth - UI_S(24);
      gbRow.Height := UI_S(178);
      gbRow.Caption := 'Header fields';

      // Row 1: ProjectID
      lblTitle := TLabel.Create(frm);
      lblTitle.Parent := gbRow;
      lblTitle.Left := UI_S(12);
      lblTitle.Top := UI_S(24);
      lblTitle.Caption := 'ProjectID';

      edProjectID := TEdit.Create(frm);
      edProjectID.Parent := gbRow;
      edProjectID.Left := UI_S(12);
      edProjectID.Top := UI_S(42);
      edProjectID.Width := UI_S(180);

      // Row 1: NumObjType
      lblTitle := TLabel.Create(frm);
      lblTitle.Parent := gbRow;
      lblTitle.Left := UI_S(210);
      lblTitle.Top := UI_S(24);
      lblTitle.Caption := 'NumObjType';

      cbNumObjType := TComboBox.Create(frm);
      cbNumObjType.Parent := gbRow;
      cbNumObjType.Left := UI_S(210);
      cbNumObjType.Top := UI_S(42);
      cbNumObjType.Width := UI_S(90);
      cbNumObjType.Style := csDropDownList;
      FillComboIntRange(cbNumObjType, 1, 5, 1);

      // Row 1: NumObjName
      lblTitle := TLabel.Create(frm);
      lblTitle.Parent := gbRow;
      lblTitle.Left := UI_S(312);
      lblTitle.Top := UI_S(24);
      lblTitle.Caption := 'NumObjName';

      cbNumObjName := TComboBox.Create(frm);
      cbNumObjName.Parent := gbRow;
      cbNumObjName.Left := UI_S(312);
      cbNumObjName.Top := UI_S(42);
      cbNumObjName.Width := UI_S(90);
      cbNumObjName.Style := csDropDownList;
      FillComboIntRange(cbNumObjName, 1, 99, 1);

      // Row 1: NumSlot
      lblTitle := TLabel.Create(frm);
      lblTitle.Parent := gbRow;
      lblTitle.Left := UI_S(414);
      lblTitle.Top := UI_S(24);
      lblTitle.Caption := 'NumSlot';

      cbNumSlot := TComboBox.Create(frm);
      cbNumSlot.Parent := gbRow;
      cbNumSlot.Left := UI_S(414);
      cbNumSlot.Top := UI_S(42);
      cbNumSlot.Width := UI_S(80);
      cbNumSlot.Style := csDropDownList;
      FillComboIntRange(cbNumSlot, 1, 20, 1);

      // Row 1: NumOpt
      lblTitle := TLabel.Create(frm);
      lblTitle.Parent := gbRow;
      lblTitle.Left := UI_S(506);
      lblTitle.Top := UI_S(24);
      lblTitle.Caption := 'NumOpt';

      cbNumOpt := TComboBox.Create(frm);
      cbNumOpt.Parent := gbRow;
      cbNumOpt.Left := UI_S(506);
      cbNumOpt.Top := UI_S(42);
      cbNumOpt.Width := UI_S(80);
      cbNumOpt.Style := csDropDownList;
      FillComboIntRange(cbNumOpt, 0, 99, 1);

      // Row 2: CRIStart / CRIStep
      lblTitle := TLabel.Create(frm);
      lblTitle.Parent := gbRow;
      lblTitle.Left := UI_S(12);
      lblTitle.Top := UI_S(78);
      lblTitle.Caption := 'CRIStart (optional)';

      edCRIStart := TEdit.Create(frm);
      edCRIStart.Parent := gbRow;
      edCRIStart.Left := UI_S(12);
      edCRIStart.Top := UI_S(96);
      edCRIStart.Width := UI_S(120);

      lblTitle := TLabel.Create(frm);
      lblTitle.Parent := gbRow;
      lblTitle.Left := UI_S(150);
      lblTitle.Top := UI_S(78);
      lblTitle.Caption := 'CRIStep (required if CRIStart set)';

      edCRIStep := TEdit.Create(frm);
      edCRIStep.Parent := gbRow;
      edCRIStep.Left := UI_S(150);
      edCRIStep.Top := UI_S(96);
      edCRIStep.Width := UI_S(120);

      // Show fixed start values note
      lblTitle := TLabel.Create(frm);
      lblTitle.Parent := gbRow;
      lblTitle.Left := UI_S(300);
      lblTitle.Top := UI_S(90);
      lblTitle.AutoSize := False;
      lblTitle.Width := gbRow.Width - UI_S(312);
      lblTitle.Height := UI_S(48);
      lblTitle.WordWrap := True;
      lblTitle.Font.Color := clGrayText;
      lblTitle.Caption :=
        'Starts fixed at 00: ObjTypeStart=00, ObjNameStart=00, SlotStart=00, OptStart=00.';

      gbPairs := TGroupBox.Create(frm);
      gbPairs.Parent := frm;
      gbPairs.Left := UI_S(12);
      gbPairs.Top := gbRow.Top + gbRow.Height + UI_S(10);
      gbPairs.Width := frm.ClientWidth - UI_S(24);
      gbPairs.Height := UI_S(210);
      gbPairs.Caption := 'BGSM swap pairs';

      gBuildInputPairsRG := TRadioGroup.Create(frm);
      gBuildInputPairsRG.Parent := gbPairs;
      gBuildInputPairsRG.Left := UI_S(10);
      gBuildInputPairsRG.Top := UI_S(18);
      gBuildInputPairsRG.Width := gbPairs.Width - UI_S(20);
      gBuildInputPairsRG.Height := UI_S(64);
      gBuildInputPairsRG.Columns := 1;
      gBuildInputPairsRG.Items.Add('Default pairs from label.csv (Base1 empty; MSWP builds default path)');
      gBuildInputPairsRG.Items.Add('Explicit Base1/Dest1 (type or browse for .bgsm path)');
      gBuildInputPairsRG.ItemIndex := 0;
      gBuildInputPairsRG.OnClick := BuildInput_PairsClick;

      // Base1 / Dest1 input (single pair for UI entry).
      lblTitle := TLabel.Create(frm);
      lblTitle.Parent := gbPairs;
      lblTitle.Left := UI_S(12);
      lblTitle.Top := UI_S(90);
      lblTitle.Caption := 'Base1 (required when explicit pairs selected)';

      edBase1 := TEdit.Create(frm);
      edBase1.Parent := gbPairs;
      edBase1.Left := UI_S(12);
      edBase1.Top := UI_S(110);
      edBase1.Width := gbPairs.Width - UI_S(70);

      btnBrowseBase1 := TButton.Create(frm);
      btnBrowseBase1.Parent := gbPairs;
      btnBrowseBase1.Left := edBase1.Left + edBase1.Width + UI_S(6);
      btnBrowseBase1.Top := edBase1.Top - UI_S(1);
      btnBrowseBase1.Width := UI_S(36);
      btnBrowseBase1.Caption := '...';
      btnBrowseBase1.OnClick := BuildInput_BrowseBaseClick;

      lblTitle := TLabel.Create(frm);
      lblTitle.Parent := gbPairs;
      lblTitle.Left := UI_S(12);
      lblTitle.Top := UI_S(144);
      lblTitle.Caption := 'Dest1 (optional; defaults to Base1 when blank)';

      edDest1 := TEdit.Create(frm);
      edDest1.Parent := gbPairs;
      edDest1.Left := UI_S(12);
      edDest1.Top := UI_S(164);
      edDest1.Width := gbPairs.Width - UI_S(70);

      btnBrowseDest1 := TButton.Create(frm);
      btnBrowseDest1.Parent := gbPairs;
      btnBrowseDest1.Left := edDest1.Left + edDest1.Width + UI_S(6);
      btnBrowseDest1.Top := edDest1.Top - UI_S(1);
      btnBrowseDest1.Width := UI_S(36);
      btnBrowseDest1.Caption := '...';
      btnBrowseDest1.OnClick := BuildInput_BrowseDestClick;

      // Wire globals used by BuildInput_PairsClick for enable/disable.
      gBuildInputBaseEdit := edBase1;
      gBuildInputDestEdit := edDest1;
      gBuildInputBaseBtn := btnBrowseBase1;
      gBuildInputDestBtn := btnBrowseDest1;
      BuildInput_PairsClick(nil);

      // Upload link (acts like hyperlink)
      lblUpload := TLabel.Create(frm);
      lblUpload.Parent := frm;
      lblUpload.Left := UI_S(12);
      lblUpload.Top := gbPairs.Top + gbPairs.Height + UI_S(10);
      lblUpload.AutoSize := True;
      lblUpload.Caption := 'For multi-row inputs: Upload a CSV FILE using the same headers.';
      lblUpload.ParentFont := True;
      lblUpload.Font.Color := clBlue;
      lblUpload.Font.Style := lblUpload.Font.Style + [fsUnderline];
      lblUpload.OnClick := BuildInput_UploadClick;

      UI_AddOkCancel(frm, lblUpload.Top + UI_S(34), btnOk, btnCancel);

      modalRes := frm.ShowModal;

      if modalRes = MR_REOPEN then begin
        reopen := True;
        Result := False;
      end else if modalRes = mrOk then begin
        if gBuildInputUseCSV then begin
          // caller will open build.csv file selection dialog
          Result := False;
          Exit;
        end;

        // Validate + build CSV in-memory
        if Trim(edProjectID.Text) = '' then begin
          MessageDlg('ProjectID is required.', mtError, [mbOK], 0);
          Result := False;
          Exit;
        end;

        numObjType := StrToIntDef(Trim(cbNumObjType.Text), 1);
        numObjName := StrToIntDef(Trim(cbNumObjName.Text), 1);
        numSlot := StrToIntDef(Trim(cbNumSlot.Text), 1);
        numOpt := StrToIntDef(Trim(cbNumOpt.Text), 1);

        if (numObjType < 1) or (numObjType > 5) then begin
          MessageDlg('NumObjType must be between 1 and 5.', mtError, [mbOK], 0);
          Result := False;
          Exit;
        end;

        if (numObjName < 1) or (numObjName > 99) or
           (numSlot < 1) or (numSlot > 99) or
           (numOpt < 0) or (numOpt > 99) then begin
          MessageDlg('NumObjName / NumSlot / NumOpt must be between 1 and 99.', mtError, [mbOK], 0);
          Result := False;
          Exit;
        end;

        criStartStr := Trim(edCRIStart.Text);
        criStepStr := Trim(edCRIStep.Text);
        if (criStartStr <> '') and (criStepStr = '') then begin
          MessageDlg('CRIStep is required when CRIStart is set.', mtError, [mbOK], 0);
          Result := False;
          Exit;
        end;

        // UI supports exactly one Base1/Dest1 pair (or blank Base1 for default pairs).
        if gBuildInputPairsRG.ItemIndex = 1 then begin
          baseS := Trim(edBase1.Text);
          destS := Trim(edDest1.Text);
          if baseS = '' then begin
            MessageDlg('Explicit pairs selected, but Base1 is empty.', mtError, [mbOK], 0);
            Result := False;
            Exit;
          end;
        end else begin
          baseS := '';
          destS := '';
        end;

        // Always emit a minimal header with Base1/Dest1, even if blank (default pairs mode)
        header := 'ProjectID,ObjTypeStart,NumObjType,ObjNameStart,NumObjName,SlotStart,NumSlot,OptStart,NumOpt,CRIStart,CRIStep,Base1,Dest1';

        row := Trim(edProjectID.Text) + ',00,' + IntToStr(numObjType) + ',00,' + IntToStr(numObjName) +
               ',00,' + IntToStr(numSlot) + ',00,' + IntToStr(numOpt) + ',' + criStartStr + ',' + criStepStr;

        if gBuildInputPairsRG.ItemIndex = 0 then begin
          // default pairs => Base1 empty, Dest1 empty
          row := row + ',,';
        end else begin
          // explicit single pair from UI (Dest may be blank => defaults later)
          row := row + ',' + baseS + ',' + destS;
        end;

        slOut.Clear;
        slOut.Add(header);
        slOut.Add(row);
        Result := True;
        Exit;
      end;

      // Cancel / close
      Result := False;

    finally
      gBuildInputForm := nil;
      gBuildInputPairsRG := nil;
      gBuildInputBaseEdit := nil;
      gBuildInputDestEdit := nil;
      gBuildInputBaseBtn := nil;
      gBuildInputDestBtn := nil;
      gUIScaleOwnerForm := nil;
      frm.Free;
    end;

  end;
end;

function OptionsDialog: boolean;
var
  frm: TForm;
  rgMode: TRadioGroup;
  rgMSWP: TRadioGroup;
  rgOMOD: TRadioGroup;
  rgCOBJ: TRadioGroup;
  rgMISC: TRadioGroup;
  gbMode, gbBuild: TGroupBox;
  lblInfo: TLabel;
  btnOk, btnCancel: TButton;

  yTop: integer;
  reopen: boolean;
  modalRes: integer;
begin
  Result := False;

  reopen := True;

  while reopen do begin
    reopen := False;
    modalRes := mrCancel;

    frm := TForm.Create(nil);
    try
      UI_InitDialogForm(frm, 700, 330, 'Generator + Labeler Options', yTop);

      lblInfo := TLabel.Create(frm);
      lblInfo.Parent := frm;
      lblInfo.Left := UI_S(12);
      lblInfo.Top := yTop;
      lblInfo.AutoSize := False;
      lblInfo.WordWrap := True;
      lblInfo.Anchors := [akLeft, akTop, akRight];
      lblInfo.Width := frm.ClientWidth - UI_S(24);
      lblInfo.Height := UI_S(44);
      lblInfo.Caption :=
        'Choose the mode and which optional record types to (re)build. ' +
        'CSV file selection happens in the next dialogs.';

      gbMode := TGroupBox.Create(frm);
      gbMode.Parent := frm;
      gbMode.Left := UI_S(12);
      gbMode.Top := lblInfo.Top + lblInfo.Height + UI_S(4);
      gbMode.Width := frm.ClientWidth - UI_S(24);
      gbMode.Height := UI_S(92);
      gbMode.Caption := 'Mode';

      rgMode := TRadioGroup.Create(frm);
      rgMode.Parent := gbMode;
      rgMode.Left := UI_S(10);
      rgMode.Top := UI_S(18);
      rgMode.Width := gbMode.Width - UI_S(20);
      rgMode.Height := gbMode.Height - UI_S(28);
      rgMode.Columns := 1;
      rgMode.Items.Add('BUILD AND LABEL');
      rgMode.Items.Add('UPDATE OR APPEND');
      rgMode.ItemIndex := 1; // default to safe pass

      gbBuild := TGroupBox.Create(frm);
      gbBuild.Parent := frm;
      gbBuild.Left := UI_S(12);
      gbBuild.Top := gbMode.Top + gbMode.Height + UI_S(10);
      gbBuild.Width := frm.ClientWidth - UI_S(24);
      gbBuild.Height := UI_S(140);
      gbBuild.Caption := 'Optional record creation (BUILD step)';

      rgMSWP := TRadioGroup.Create(frm);
      rgMSWP.Parent := gbBuild;
      rgMSWP.SetBounds(UI_S(10), UI_S(16), (gbBuild.Width div 4) - UI_S(14), UI_S(102));
      rgMSWP.Caption := 'MSWP';
      rgMSWP.Items.Add('Build/overwrite');
      rgMSWP.Items.Add('Skip');
      rgMSWP.ItemIndex := 0;
      rgMSWP.OnClick := OptionsDialog_MSWPClick;

      rgOMOD := TRadioGroup.Create(frm);
      rgOMOD.Parent := gbBuild;
      rgOMOD.SetBounds(rgMSWP.Left + rgMSWP.Width + UI_S(6), UI_S(16), (gbBuild.Width div 4) - UI_S(14), UI_S(102));
      rgOMOD.Caption := 'OMOD';
      rgOMOD.Items.Add('Build/overwrite');
      rgOMOD.Items.Add('Skip');
      rgOMOD.ItemIndex := 0;
      rgOMOD.OnClick := OptionsDialog_OMODClick;

      rgCOBJ := TRadioGroup.Create(frm);
      rgCOBJ.Parent := gbBuild;
      rgCOBJ.SetBounds(rgOMOD.Left + rgOMOD.Width + UI_S(6), UI_S(16), (gbBuild.Width div 4) - UI_S(14), UI_S(102));
      rgCOBJ.Caption := 'COBJ';
      rgCOBJ.Items.Add('Build/overwrite');
      rgCOBJ.Items.Add('Skip');
      rgCOBJ.ItemIndex := 1;
      rgCOBJ.OnClick := OptionsDialog_COBJClick;

      rgMISC := TRadioGroup.Create(frm);
      rgMISC.Parent := gbBuild;
      rgMISC.SetBounds(rgCOBJ.Left + rgCOBJ.Width + UI_S(6), UI_S(16), (gbBuild.Width div 4) - UI_S(14), UI_S(102));
      rgMISC.Caption := 'MISC';
      rgMISC.Items.Add('Build/overwrite');
      rgMISC.Items.Add('Skip');
      rgMISC.ItemIndex := 1;
	  rgMISC.OnClick := OptionsDialog_MISCClick;

      // Wire globals for dependency refresh.
      gDlgRGMSWP := rgMSWP;
      gDlgRGOMOD := rgOMOD;
      gDlgRGCOBJ := rgCOBJ;
      gDlgRGMISC := rgMISC;

      OptionsDialog_Refresh;

      UI_AddOkCancel(frm, gbBuild.Top + gbBuild.Height + UI_S(16), btnOk, btnCancel);

      modalRes := frm.ShowModal();

      if modalRes = MR_REOPEN then begin
        reopen := True;
        Result := False;
      end else if modalRes = mrOk then begin
        if rgMode.ItemIndex = 0 then
          gMode := MODE_BUILD_LABEL
        else
          gMode := MODE_REVERT_BUILD_LABEL;

        gDoBuildMSWP := rgMSWP.ItemIndex = 0;
        gDoBuildOMOD := rgOMOD.ItemIndex = 0;
        // Allow COBJ/MISC rebuilds independent of OMOD (enables "COBJ-only" maintenance runs).
        gDoBuildCOBJ := rgCOBJ.ItemIndex = 0;
        gDoBuildMISC := rgMISC.ItemIndex = 0;
        Result := True;
        Exit;
      end;

      Result := False;

    finally
      gDlgRGMSWP := nil;
      gDlgRGOMOD := nil;
      gDlgRGCOBJ := nil;
      gDlgRGMISC := nil;
      gUIScaleOwnerForm := nil;
      frm.Free;
    end;

  end;
end;



// ============================================================================
// [RUN] Entry point
// ============================================================================
// @anchor RUN_Initialize
function Initialize: integer;
var
  buildLines: TStringList;
  m: integer;
begin
  Result := 0;
  AddMessage('IRG PATCHED24 loaded: MSWP create fallback + Opt=0 support + hasCRI fix');
  if not (wbGameMode = gmFO4) then begin
    AddMessage('This script only supports Fallout 4');
    Result := -1;
    Exit;
  end;

  // UI scaling (per-run). Default to 100%; user can change in any dialog via the embedded zoom selector.
  gUITextScalePct := 100;

  // Optional startup splash screens shown before any other UI.
// Place PNGs at:
//   <xEdit folder>\Edit Scripts\FO4_CSV_Generator_Labeler\splash1.png   (fallback: splash.png)
//   <xEdit folder>\Edit Scripts\FO4_CSV_Generator_Labeler\splash2.png
UI_ShowStartupSplashes;

  gTargetFile := FileSelect;
  if not Assigned(gTargetFile) then begin
    AddMessage('No target file was selected.');
    Result := -1;
    Exit;
  end;

  // Ensure Fallout4.esm is a master (needed for MISC template copy)
  try
    AddMasterIfMissing(gTargetFile, GetFileName(FileByIndex(0)));
  except
    // ignore
  end;

  if not OptionsDialog then begin
    AddMessage('Cancelled.');
    Result := -1;
    Exit;
  end;

  // [LBL] label.csv optional BGSM pair overrides (per-type and per-record)
  gPairsObjType := TStringList.Create;
  gPairsObjName := TStringList.Create;
  gPairsSlot := TStringList.Create;
  gPairsOpt := TStringList.Create;
  gPairsMSWP := TStringList.Create;
  gPairsObjType.NameValueSeparator := '=';
  gPairsObjName.NameValueSeparator := '=';
  gPairsSlot.NameValueSeparator := '=';
  gPairsOpt.NameValueSeparator := '=';
  gPairsMSWP.NameValueSeparator := '=';

  // [COBJ] optional cobjrecipes.csv overrides
  ClearCobjRecipeMaps;

gSIGSInit := False;

  // Standalone "COBJ-only (physical items)" build.
  // Detected by inspecting the first real "Type" token in the selected label CSV:
  // - WorkbenchName/CatName/ItemName => standalone physical-item COBJ workflow (no build input UI)
  // - ObjType/ObjName/Slot/Opt/MSWP  => standard workflow (uses build inputs + standard label.csv parsing).
  buildLines := TStringList.Create;
  try
    // Preload label.csv first so we can decide whether the build-input UI is relevant.
    if Assigned(gPreloadedLabelLines) then begin
      gPreloadedLabelLines.Free;
      gPreloadedLabelLines := nil;
    end;
    gPreloadedLabelLines := TStringList.Create;
    gPreloadedLabelKind := LBL_KIND_UNKNOWN;

    // Select label CSV. If the user chose an incompatible format for their options, prompt again.
    while True do begin
      gPreloadedLabelLines.Clear;
      if not LoadCSVWithDialog(gPreloadedLabelLines, 'Select label.csv (or COBJ-only label CSV for physical items)', True) then begin
        Result := -1;
        Exit;
      end;

      gLastLabelCSVPath := gLastCSVDialogFilename;
      gPreloadedLabelKind := DetectLabelCSVKind(gPreloadedLabelLines);

      // If the user selected to build OMODs (or MISC), a standalone COBJ-only label CSV is invalid.
      if (gPreloadedLabelKind = LBL_KIND_COBJ_ONLY) and (gDoBuildOMOD or gDoBuildMISC) then begin
        MessageDlg('Invalid label CSV for selected options.' + #13#10 +
                   'You enabled OMOD/MISC building, which requires a standard label.csv containing OBJTYPE/OBJNAME/SLOT/OPT (and optionally MSWP).' + #13#10 +
                   'The COBJ-only physical-item label CSV (WORKBENCHNAME/CATNAME/ITEMNAME) is only valid for standalone created-item runs with OMOD/MISC disabled.' + #13#10 +
                   'Please select a standard label.csv, or Cancel and rerun with OMOD/MISC disabled.', mtError, [mbOK], 0);
        Continue;
      end;

      Break;
    end;

    // Standalone COBJ-only (physical item) label CSV: skip build input entirely.
    if gPreloadedLabelKind = LBL_KIND_COBJ_ONLY then begin
      AddMessage('--- COBJ-ONLY (PHYSICAL ITEMS) RUN ---');
      AddMessage('NOTE: Mode selection is ignored for standalone physical-item runs (no revert/build.csv pass).');

      if not gDoBuildCOBJ then begin
        AddMessage('NOTE: COBJ build was disabled in options; enabling it for this standalone run.');
        gDoBuildCOBJ := True;
      end;

      if gDoBuildOMOD or gDoBuildMISC then
        AddMessage('NOTE: Standalone COBJ-only label CSV detected; ignoring OMOD/MISC options and skipping build input.');
      gDoBuildMSWP := False;
      gDoBuildOMOD := False;
      gDoBuildMISC := False;

      if not LoadStandaloneCobjLabelCSVAndBuild then
        Result := -1
      else
        Result := 0;
      Exit;
    end;

    // Manual-first build input dialog. User may click link to upload a CSV instead.

    if not BuildInputDialog(buildLines) then begin
      if gBuildInputUseCSV then begin
        buildLines.Clear;
        if not LoadCSVWithDialog(buildLines, 'Select REQUIRED build.csv', True) then begin
          AddMessage('build.csv load cancelled/failed.');
          Result := -1;
          Exit;
        end;

      end else begin
        AddMessage('build input cancelled.');
        Result := -1;
        Exit;
      end;
    end;


    // Parse label.csv (already selected above).
    if not LoadLabelCSVRequired then begin
      AddMessage('label.csv load cancelled/failed.');
      Result := -1;
      Exit;
    end;

    // Optional: load cobjrecipes.csv to define OutputCount/components overrides for COBJ recipes.
    // (FNAM/CategoryKywd is not read from this file.)
    if gDoBuildCOBJ then begin
      LoadCobjRecipesCSVOptional; // cancel is OK; recipes remain minimal
    end;

    // Mode sequencing
    if gMode = MODE_REVERT_BUILD_LABEL then begin
      AddMessage('--- REVERT PASS ---');
      RevertPass(buildLines);
    end;

    AddMessage('--- BUILD PASS ---');
    BuildPass(buildLines);

    AddMessage('--- LABEL PASS ---');
    LabelPass(buildLines);

    AddMessage('Done.');

  finally
    buildLines.Free;
    if Assigned(gPreloadedLabelLines) then begin
      gPreloadedLabelLines.Free;
      gPreloadedLabelLines := nil;
    end;
    gPreloadedLabelKind := LBL_KIND_UNKNOWN;

    if Assigned(gPairsMSWP) then begin gPairsMSWP.Free; gPairsMSWP := nil; end;
    if Assigned(gPairsOpt) then begin gPairsOpt.Free; gPairsOpt := nil; end;
    if Assigned(gPairsSlot) then begin gPairsSlot.Free; gPairsSlot := nil; end;
    if Assigned(gPairsObjName) then begin gPairsObjName.Free; gPairsObjName := nil; end;
    if Assigned(gPairsObjType) then begin gPairsObjType.Free; gPairsObjType := nil; end;
      // free [COBJ] recipe override maps
      for m := 0 to 15 do begin
        if Assigned(gCobjRecipeByMask[m]) then begin
          gCobjRecipeByMask[m].Free;
          gCobjRecipeByMask[m] := nil;
        end;
      end;
end;
end;

end.
