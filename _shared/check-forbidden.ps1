# FBD (2026-10-06): forbidden-pattern check (README section 8, forbidden list).
# Called by _shared\CommonMod.props before building ContainerSuite.Infrastructure.
# Exit 0 = clean, exit 1 = a forbidden pattern was found.
#
# Currently enforced (five rules that are mechanically checkable):
#
#   R1  forbidden #21 (revised 2026-10-07) -- EmporiumEntry.Validate(...) must
#       pass the named constant SaveBagExpandCore.EmporiumForceValidate.
#       Bare boolean literals true/false are forbidden.
#
#       * Why the old R1 ("no argument allowed") was wrong:
#         the old rule assumed "Validate() with no arg == Validate(false)".
#         Triple evidence (ilspycmd + dump.cs + compiled IL of the deployed
#         DLL) proves that is FALSE: EmporiumEntry.Validate(bool force = true)
#         has metadata default TRUE, and C# inserts the default at the CALL
#         SITE, so `Validate()` compiles to `ldc.i4.1; callvirt Validate(bool)`
#         -- identical to `Validate(true)`.
#         => the old R1 protected nothing AND blocked the only way to
#            actually express "non-forced" (`Validate(false)`).
#         See README forbidden item #21 and the doc comment on
#         SaveBagExpandCore.EmporiumForceValidate.
#
#       R1a: the argument of .Validate(<arg>) must not be a bare bool literal.
#       R1b: SaveBagExpandPatch.cs must contain >= 2 occurrences of
#            EmporiumForceValidate (both EmporiumEntry call sites go through
#            the named constant; reverting to a bare call fails the check).
#
#   R2  forbidden #22 -- grid-destructive ops during load.
#       .Expel / .SetShape / .UncheckedAccept / ForceRecomputeDynamicBackground
#       / .ClearGrid during WorldReadyGate.ShouldDefer triggers
#       Internal CLR error 0x80131506 (S8u incident, not catchable managed).
#       "Will this run during load" is not statically decidable, so the rule
#       is file-level: a file doing such ops must reference WorldReadyGate,
#       otherwise it must be listed in $allowGate below (a listing records the
#       2026-10-06 status, it is NOT proof of safety; adding one requires
#        manual review). A new file with neither gate nor listing fails build.
#
#   R3  forbidden #25 -- RuntimeHelpers.PrepareMethod (Mode B JIT compile).
#       PrepareMethod forces a full-optimization JIT compile through MonoMod
#       DetourRuntimeNETCore30Platform.CompileMethodHook. Under IL2CPP
#       MelonLoader that hook hits Internal CLR error 0x80131506, a FailFast
#       that try/catch cannot catch (process dies with no log). 0.46D
#       2026-10-08: SaveBagPaging.WarmupJit group 5 printed
#       "Prep CustomInfrastructure: 689 methods" and died 8 ms later.
#       Default-deny: a file mentioning RuntimeHelpers.PrepareMethod must be in
#       $prepAllow, else build fails. A listing records review status only -- it
#       is NOT proof of safety (same caveat as $allowGate). Single-method and
#       whole-assembly sweeps are treated alike: both are Mode B.
#
#   R4  forbidden #26 -- hot accessor (get_/set_) patches.
#       A HarmonyPatch targeting a property accessor by name is a HOT patch
#       point: IL2CPP auto-property accessors are trivial (return this[+off])
#       and are read thousands of times per frame. Detouring them can hit the
#       same MonoMod CompileMethodHook FailFast (0x80131506) as R3. 0.46D
#       2026-10-09: the read-only resize probe patched set_widthPixels /
#       set_heightPixels (accessors) and the process hard-crashed right after
#       all mods loaded (log stopped, no OnApplicationQuit). Default-deny at
#       pair level: a "Type::get_X" pair must be listed in $hotAllow below.
#       File-agnostic and pair-exact, so it fires on any NEW hot accessor patch
#       anywhere but leaves the reviewed 4 layout getters alone. A listing
#       records the 2026-10-09 review only -- it is NOT proof of safety, and it
#       does NOT license adding new hot accessor patches.
#
#   R7  single-source cfg defaults (2026-10-10, P0-1).
#       Disease it prevents: key 3D_SweepZombieHandles was created in 3 files
#       with 2 CONTRADICTORY default values (Core.cs said true, the sweep
#       comment said false), so no reader could tell which default was
#       authoritative. Rule: a fully-literal CreateEntry key (the string is the
#       first argument, e.g. CreateEntry<bool>("KEY", ...)) may appear at most
#       ONCE across the 4 managed projects. Files that merely need the entry
#       must reference the created field, not re-create the key. Dynamic keys
#       (first arg built from an expression, e.g. prefix + "Ids") are skipped:
#       they are not literal and cannot be attributed to a single source here.
#
#   R5  manual patch warmup registration (2026-10-10, P1-2).
#       A manual harmony.Patch(...) installs a patch whose FIRST real invocation
#       may land on a deep load-time stack; if that method was never JITed the
#       MonoMod CompileMethodHook FailFast (0x80131506, uncatchable) kills the
#       process. The project therefore requires every manually patched method to
#       be registered via WarmupRegistry.Register, so it is JIT-warmed on a
#       shallow stack. Rule: a file whose CODE (line comments stripped) contains
#       "harmony.Patch(" must also contain "WarmupRegistry.Register(". The
#       registry file itself only mentions harmony.Patch in a doc comment, so
#       stripping comments removes the need to exempt it.
#
#   R8  no retained dead-code / diff-artifact comments (2026-10-10, P0-3).
#       Convention: source comments state why the CURRENT code is like this;
#       historical decisions and commented-out implementations belong in git
#       or _archive\, not in the .cs. Three markers that unambiguously mean
#       "an old implementation was kept in-source on purpose":
#         - "keep the block to shrink the diff"
#         - "keep the full old implementation below"
#         - "copy it back into Tick()"
#       (CJK needles are built from code points below so this file stays pure
#       ASCII, per the PS 5.1 encoding note.) Hit => build fails; if the text
#       is genuinely needed, move it to _archive\ and leave a pointer comment.
#
#   R6  cache registration coverage (2026-10-10, P1-1).
#       Disease it prevents: a rebuildable cache field that nobody clears on
#       scene change. Scene change = world reset (IL2CPP reuses addresses), so
#       an uncleared pointer-keyed cache makes the NEW object hit the OLD
#       scene's entry -- silent, and very hard to trace. Rule: every
#       "private static readonly Dictionary<...> / HashSet<...>" field whose
#       name ends with "Cache" or "Seen" must appear either as the field-name
#       argument of a CacheRegistry.Register("<field>", ...) call somewhere in the
#       4 managed projects (the registration in DiyUpgradeMod.RegisterSceneCaches
#       wires the actual clear action) OR in the CacheRegistry.KeepUncleared
#       listing (a cache reviewed as process-lifetime). Missing from both =>
#       build fails, so adding a new cache is a compile error until it is
#       registered or explicitly reviewed. Suffix matching is case-sensitive (the
#       project's convention is _xxxCache / _diagSeen); _cache (TypeCache) and
#       _seen (CatchLogOnce) are process-lifetime sets and are intentionally out
#       of scope. Listing in KeepUncleared records the review only -- it is NOT
#       proof of safety, and it does NOT license skipping review for new caches.
#
# Style rules (same as check-suite-version.ps1):
#  - Pure ASCII ONLY. Do NOT add non-ASCII comments: PS 5.1 reads this file
#    without BOM as ANSI, and multi-byte chars turn into mojibake that can
#    break parsing (hit on 2026-10-07 with a CJK comment -> UnexpectedToken).
#  - No regex beyond Split for line endings; no backslash escapes.

param(
    [Parameter(Mandatory = $true)]
    [string]$WorkspaceRoot
)

$ErrorActionPreference = 'Stop'

# Only the 4 managed projects (the workspace also holds unrelated projects).
$projectDirs = @(
    'ContainerSuite.Shared',
    'ContainerSuite.Infrastructure',
    'ContainerSuite.SaveBag',
    'ContainerSuite.DIYUpgrade'
)

# Existing files that do grid-destructive ops but never reference WorldReadyGate.
$allowGate = @(
    'ContainerExpander.cs',
    'DiyUpgradeMod.GridFx.cs',
    'DiyUpgradeMod.Upgrade.cs'
)

# #25: files allowed to use RuntimeHelpers.PrepareMethod (Mode B JIT compile).
# Default-deny: any other file using it fails the build. A listing records
# manual review status only -- it is NOT proof of safety (same caveat as
# $allowGate). Currently empty: the only user (SaveBagPaging.cs WarmupJit)
# had its Mode B groups/probe deleted, so R3 is zero-exemption.
$prepAllow = @()

# #26: hot accessor patches allowed by name (Type::get_X / Type::set_X).
# Default-deny: any HarmonyPatch whose quoted method name starts with get_ or
# set_ must have its "Type::method" pair listed here, else the build fails.
# Why: IL2CPP auto-property accessors are trivial (return this[+off]) and are
# read thousands of times per frame; detouring them can hit the same
# MonoMod DetourRuntimeILPlatform.CompileMethodHook FailFast (0x80131506,
# managed catch cannot catch) that killed the 2026-10-09 read-only resize
# probe. The 4 entries below are the load-bearing layout getters already
# shipped and field-verified across many versions (DiyUpgradeMod.Patches.cs);
# they are also depended on by SetShapeKeepTransparency, so they cannot be
# gated out. A listing records the 2026-10-09 review -- it is NOT blanket
# proof of safety, and it does NOT license adding NEW hot accessor patches.
$hotAllow = @(
    'PixelWindow::get_widthPixels',
    'PixelWindow::get_heightPixels',
    'GameGridInventory::get_widthPixels',
    'GameGridInventory::get_heightPixels'
)

# #22: operations that mutate/expel grid contents during load
$ops = @('.Expel(', '.SetShape(', '.UncheckedAccept(', 'ForceRecomputeDynamicBackground', '.ClearGrid(')

$enc = New-Object System.Text.UTF8Encoding($false)
$bad = @()
$fileCount = 0
$entryKeys = @{}
$cacheFields = @{}
$registeredFields = @{}
$keptFields = @{}

# R8 needles, built from Unicode code points to keep this file pure ASCII.
# cp1 = U+4FDD U+7559 U+5757 U+4EE5 U+7F29 U+5C0F  (bao-liu-kuai-yi-suo-xiao)
# cp2 = U+4FDD U+7559 U+539F U+5B9E U+73B0 U+5168 U+6587 (bao-liu-yuan-shi-xian-quan-wen)
# cp3 = U+7167 U+6284 U+56DE  (zhao-chao-hui)
$deadNeedles = @(
    ([string][char]0x4FDD + [string][char]0x7559 + [string][char]0x5757 + [string][char]0x4EE5 + [string][char]0x7F29 + [string][char]0x5C0F),
    ([string][char]0x4FDD + [string][char]0x7559 + [string][char]0x539F + [string][char]0x5B9E + [string][char]0x73B0 + [string][char]0x5168 + [string][char]0x6587),
    ([string][char]0x7167 + [string][char]0x6284 + [string][char]0x56DE)
)

foreach ($dir in $projectDirs) {
    $full = Join-Path $WorkspaceRoot $dir
    if (-not (Test-Path $full)) { continue }

    $files = Get-ChildItem -Path $full -Recurse -Filter '*.cs' -File |
        Where-Object { $_.FullName -notlike '*\bin\*' -and $_.FullName -notlike '*\obj\*' }

    foreach ($f in $files) {
        $fileCount++
        $text = [System.IO.File]::ReadAllText($f.FullName, $enc)
        $lines = [System.IO.File]::ReadAllLines($f.FullName, $enc)

        # Comment-stripped code view (line comments only) for code-shape rules (R5).
        # Block comments are not stripped; none of the managed files hide
        # harmony.Patch( inside /* */, so line-level stripping is enough here.
        $codeOnly = ''
        foreach ($ln2 in $lines) {
            $c2 = $ln2
            $ci2 = $c2.IndexOf('//')
            if ($ci2 -ge 0) { $c2 = $c2.Substring(0, $ci2) }
            $codeOnly += $c2 + "`n"
        }

        # --- R1a: .Validate(<arg>) must not take a bare bool literal (comments ignored) ---
        for ($i = 0; $i -lt $lines.Length; $i++) {
            $code = $lines[$i]
            $cpos = $code.IndexOf('//')
            if ($cpos -ge 0) { $code = $code.Substring(0, $cpos) }

            $p = $code.IndexOf('.Validate(')
            if ($p -lt 0) { continue }

            $after = $code.Substring($p + '.Validate('.Length).Trim()
            if ($after.Length -eq 0) { continue }
            if ($after.Substring(0, 1) -eq ')') { continue }   # no-arg call (PixelWindow / GameInventory)

            $low = $after.ToLowerInvariant()
            if ($low.StartsWith('true') -or $low.StartsWith('false')) {
                $bad += ('R1a(#21) ' + $f.Name + ':' + ($i + 1) +
                         '  EmporiumEntry.Validate must pass the named constant ' +
                         'EmporiumForceValidate; bare boolean literal not allowed. Found: ' +
                         $lines[$i].Trim())
            }
        }

        # --- R1b: both EmporiumEntry call sites in SaveBagExpandPatch.cs must use the constant ---
        #  a bare Validate() equals Validate(true) (metadata default is true); reverting fails.
        if ($f.Name -eq 'SaveBagExpandPatch.cs') {
            $cnt = 0
            foreach ($ln in $lines) {
                $c = $ln.IndexOf('//')
                if ($c -ge 0) { $ln = $ln.Substring(0, $c) }
                if ($ln.IndexOf('EmporiumForceValidate') -ge 0) { $cnt++ }
            }
            if ($cnt -lt 2) {
                $bad += ('R1b(#21) SaveBagExpandPatch.cs: EmporiumForceValidate appears ' + $cnt +
                         ' times (<2). Both EmporiumEntry.Validate call sites must pass it ' +
                         'explicitly; bare Validate() == Validate(true).')
            }
        }

        # --- R7: collect fully-literal CreateEntry key sites (reported after the loop) ---
        #   Only the literal-first-argument form counts: between '(' and the opening
        #   quote there must be nothing but spaces. That skips dynamic keys such as
        #   CreateEntry<bool>(prefix + "Enable", ...) which have no single source.
        for ($i = 0; $i -lt $lines.Length; $i++) {
            $line = $lines[$i]
            $k = $line.IndexOf('.CreateEntry<')
            if ($k -lt 0) { continue }
            $gt = $line.IndexOf('>', $k)
            if ($gt -lt 0) { continue }
            $p = $line.IndexOf('(', $gt)
            if ($p -lt 0) { continue }
            $q1 = $line.IndexOf('"', $p)
            if ($q1 -lt 0) { continue }
            if ($line.Substring($p + 1, $q1 - $p - 1).Trim().Length -ne 0) { continue }
            $q2 = $line.IndexOf('"', $q1 + 1)
            if ($q2 -lt 0) { continue }
            $keyName = $line.Substring($q1 + 1, $q2 - $q1 - 1)
            if ($keyName.Length -eq 0) { continue }
            if (-not $entryKeys.ContainsKey($keyName)) { $entryKeys[$keyName] = @() }
            $entryKeys[$keyName] += ($f.Name + ':' + ($i + 1))
        }

        # --- R6: collect cache fields + their CacheRegistry registrations (P1-1) ---
        #   Candidate: "private static readonly Dictionary<...> / HashSet<...> _x"
        #   whose field name ends with "Cache" or "Seen" (case-sensitive). The
        #   name is the first "_token" on the declaration line (the type part has
        #   no underscore); the type may wrap to the next line, so join one line
        #   for the Dictionary/HashSet test.
        for ($i = 0; $i -lt $lines.Length; $i++) {
            $ln = $lines[$i]
            if ($ln.IndexOf('private static readonly') -lt 0) { continue }
            $blob = $ln
            if ($i + 1 -lt $lines.Length) { $blob = $blob + ' ' + $lines[$i + 1] }
            if (($blob.IndexOf('Dictionary<') -lt 0) -and ($blob.IndexOf('HashSet<') -lt 0)) { continue }

            $us = $ln.IndexOf('_')
            if ($us -lt 0) { continue }
            $rest = $ln.Substring($us)
            $endIdx = $rest.Length
            foreach ($d in @(' ', '=', ';', '(', ')', ',', '>', ':')) {
                $di = $rest.IndexOf($d)
                if (($di -ge 0) -and ($di -lt $endIdx)) { $endIdx = $di }
            }
            if ($endIdx -le 0) { continue }
            $fn = $rest.Substring(0, $endIdx)

            if ($fn.EndsWith('Cache') -or $fn.EndsWith('Seen')) {
                if (-not $cacheFields.ContainsKey($fn)) { $cacheFields[$fn] = @() }
                $cacheFields[$fn] += ($f.Name + ':' + ($i + 1))
            }
        }

        # Registrations: CacheRegistry.Register("<field>", ...) -- field name is
        # the first string literal of the call.
        for ($i = 0; $i -lt $lines.Length; $i++) {
            $ln2 = $lines[$i]
            $rk = $ln2.IndexOf('CacheRegistry.Register(')
            if ($rk -lt 0) { continue }
            $rp = $ln2.IndexOf('(', $rk)
            if ($rp -lt 0) { continue }
            $rq1 = $ln2.IndexOf('"', $rp)
            if ($rq1 -lt 0) { continue }
            $rq2 = $ln2.IndexOf('"', $rq1 + 1)
            if ($rq2 -lt 0) { continue }
            $rn = $ln2.Substring($rq1 + 1, $rq2 - $rq1 - 1)
            if ($rn.Length -eq 0) { continue }
            if (-not $registeredFields.ContainsKey($rn)) { $registeredFields[$rn] = @() }
            $registeredFields[$rn] += ($f.Name + ':' + ($i + 1))
        }

        # Kept-cache listing: CacheRegistry.KeepUncleared = new string[] { "_x", ... }
        if ($f.Name -eq 'CacheRegistry.cs') {
            $inKeep = $false
            foreach ($ln3 in $lines) {
                if (-not $inKeep) {
                    if ($ln3.IndexOf('KeepUncleared') -ge 0) { $inKeep = $true }
                    continue
                }
                if ($ln3.TrimStart().StartsWith('};')) { break }
                $kq1 = $ln3.IndexOf('"')
                if ($kq1 -lt 0) { continue }
                $kq2 = $ln3.IndexOf('"', $kq1 + 1)
                if ($kq2 -lt 0) { continue }
                $kn = $ln3.Substring($kq1 + 1, $kq2 - $kq1 - 1)
                if ($kn.Length -gt 0) { $keptFields[$kn] = $true }
            }
        }

        # --- R5: manual harmony.Patch sites must register a warmup (P1-2) ---
        #   Uses the comment-stripped $codeOnly so the doc comment in
        #   WarmupRegistry.cs does not trip the rule.
        if ($codeOnly.Contains('harmony.Patch(') -and -not $codeOnly.Contains('WarmupRegistry.Register(')) {
            $bad += ('R5  ' + $f.Name + ' calls harmony.Patch(...) (a manual patch) but never ' +
                     'calls WarmupRegistry.Register(...). A manually patched method first invoked ' +
                     'on a deep load-time stack can FailFast with Internal CLR error 0x80131506 ' +
                     '(uncatchable); register it so it is JIT-warmed on the shallow stack.')
        }

        # --- R8: retained dead-code / diff-artifact comments are forbidden (P0-3) ---
        #   $text was read as UTF-8 above, so the CJK needles match correctly.
        foreach ($nd in $deadNeedles) {
            if ($text.Contains($nd)) {
                $bad += ('R8  ' + $f.Name + ' contains a retained dead-code marker (an old ' +
                         'implementation kept in-source on purpose). Comments must explain the ' +
                         'CURRENT code; move history / commented-out code to _archive\ and ' +
                         'leave a short pointer comment.')
                break
            }
        }

        # --- R3: RuntimeHelpers.PrepareMethod is default-DENY (#25) ---
        #   forbidden #25. PrepareMethod forces a full-optimization JIT compile
        #   (Mode B) through MonoMod DetourRuntimeNETCore30Platform.CompileMethodHook.
        #   Under IL2CPP MelonLoader that hook hits Internal CLR error 0x80131506,
        #   a FailFast that try/catch cannot catch: the process dies with no log.
        #   0.46D 2026-10-08: SaveBagPaging.WarmupJit group 5 printed
        #   "Prep CustomInfrastructure: 689 methods" and died 8 ms later.
        #   Mode B can never be made safe at runtime, so the rule is default-deny
        #   at file level: any file mentioning RuntimeHelpers.PrepareMethod must be
        #   listed in $prepAllow above, otherwise the build fails. This also catches
        #   whole-type sweeps (GetMethods/GetTypes) that a token-pair rule would miss.
        #   NOTE: R3 does not continue -- R2 below must still run for this file
        #   (SaveBagPaging.cs also does grid ops), so this file is not skipped.
        if ($text.Contains('RuntimeHelpers.PrepareMethod')) {
            if ($prepAllow -notcontains $f.Name) {
                $bad += ('R3(#25) ' + $f.Name + ' uses RuntimeHelpers.PrepareMethod ' +
                         '(Mode B full-optimization JIT compile) which can FailFast with ' +
                         'Internal CLR error 0x80131506 under IL2CPP MelonLoader. ' +
                         'Default-deny: remove it, or add the file to $prepAllow in ' +
                         '_shared\check-forbidden.ps1 after manual review.')
            }
        }

        # --- R4: hot accessor patches are default-DENY (#26) ---
        #   Forbidden #26. A HarmonyPatch line targeting a property accessor by
        #   name (quoted "get_..." / "set_...") is a HOT patch point: IL2CPP
        #   auto-property accessors are trivial (return this[+off]) and read
        #   thousands of times per frame. 0.46D 2026-10-09: the read-only resize
        #   probe patched PixelWindow/GameGridInventory set_widthPixels /
        #   set_heightPixels (accessors) and the process hard-crashed right
        #   after all mods loaded (log stopped, no OnApplicationQuit), matching
        #   MonoMod DetourRuntimeILPlatform.CompileMethodHook -> 0x80131506, a
        #   FailFast try/catch cannot catch. The same crash class as R3 (#25).
        #   Default-deny: a "Type::get_X" pair must be listed in $hotAllow,
        #   which records the 2026-10-09 review of the 4 layout getters.
        #   This is FILE-AGNOSTIC and pair-exact: it fires anywhere a NEW hot
        #   accessor patch is added, while leaving the reviewed 4 untouched.
        #   NOTE: R4 does not continue -- R2 below must still run for this file.
        for ($i = 0; $i -lt $lines.Length; $i++) {
            $code = $lines[$i]
            if ($code.IndexOf('HarmonyPatch(') -lt 0) { continue }
            $gi = $code.IndexOf('"get_')
            $si = $code.IndexOf('"set_')
            $q = $gi
            if ($q -lt 0) { $q = $si }
            if ($q -lt 0) { continue }   # not an accessor patch

            $tp = $code.IndexOf('typeof(')
            if ($tp -lt 0) { continue }
            $ts = $tp + 'typeof('.Length
            $te = $code.IndexOf(')', $ts)
            if ($te -lt 0) { continue }
            $typeName = $code.Substring($ts, $te - $ts)

            $ms = $q + 1
            $me = $code.IndexOf('"', $ms)
            if ($me -lt 0) { continue }
            $method = $code.Substring($ms, $me - $ms)

            $pair = ($typeName + '::' + $method)
            if ($hotAllow -notcontains $pair) {
                $bad += ('R4(#26) ' + $f.Name + ':' + ($i + 1) +
                         '  patches hot accessor ' + $pair +
                         ' (property get_/set_ is read thousands of times per frame; ' +
                         'detouring it can FailFast with Internal CLR error 0x80131506 ' +
                         'under IL2CPP MelonLoader, as the 2026-10-09 resize probe did). ' +
                         'Default-deny: remove it, or add "' + $pair + '" to $hotAllow in ' +
                         '_shared\check-forbidden.ps1 after manual review + real-machine test.')
            }
        }

        # --- R2: grid-destructive ops must live in a file that knows WorldReadyGate ---
        $hasOp = $false
        foreach ($o in $ops) { if ($text.Contains($o)) { $hasOp = $true; break } }
        if (-not $hasOp) { continue }
        if ($text.Contains('WorldReadyGate')) { continue }
        if ($allowGate -contains $f.Name) { continue }
        $bad += ('R2(#22) ' + $f.Name + ' does grid-destructive ops but never references ' +
                 'WorldReadyGate. Gate the call sites, or add the file to $allowGate ' +
                 'in _shared\check-forbidden.ps1 after manual review.')
    }
}

# --- R7 reporting: a literal cfg key must be created exactly once (P0-1) ---
foreach ($kk in @($entryKeys.Keys)) {
    if ($entryKeys[$kk].Count -gt 1) {
        $bad += ('R7  cfg key "' + $kk + '" is created ' + $entryKeys[$kk].Count +
                 ' times (' + ($entryKeys[$kk] -join ', ') + '). A key must have a single ' +
                 'CreateEntry site so its default value has one authoritative source; ' +
                 'other files must reference the created entry instead of re-creating the key.')
    }
}

# --- R6 reporting: every *Cache / *Seen static dictionary must be registered (P1-1) ---
foreach ($cf in @($cacheFields.Keys)) {
    if ($registeredFields.ContainsKey($cf)) { continue }
    if ($keptFields.ContainsKey($cf)) { continue }
    $bad += ('R6  cache field "' + $cf + '" (' + ($cacheFields[$cf] -join ', ') +
             ') is a private static readonly Dictionary/HashSet whose name ends with ' +
             'Cache/Seen, but no CacheRegistry.Register("' + $cf + '", ...) registers it. ' +
             'An unregistered cache is never cleared on scene change, so an IL2CPP ' +
             'address-reused object can be hit through a stale key. Register it in ' +
             'DiyUpgradeMod.RegisterSceneCaches() with a clear action (or, if it is ' +
             'reviewed as process-lifetime, list it in CacheRegistry.KeepUncleared).')
}

if ($bad.Count -gt 0) {
    foreach ($b in $bad) { Write-Host ('FBD: ' + $b) }
    Write-Host 'FBD: forbidden-pattern check FAILED.'
    exit 1
}

Write-Host ('FBD: forbidden-pattern check passed (' + $fileCount + ' files, rules #21 + #22 + #25 + #26 + cfg-single-source + patch-warmup + no-dead-code + cache-registry).')
exit 0
