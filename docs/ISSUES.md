# Naner Issues Tracker

This document provides guidance on tracking and managing issues for Naner development.

## Quick Links

- **Create Issue:** https://github.com/baileyrd/naner/issues/new/choose
- **View All Issues:** https://github.com/baileyrd/naner/issues
- **View Open Bugs:** https://github.com/baileyrd/naner/issues?q=is%3Aissue+is%3Aopen+label%3Abug
- **View Feature Requests:** https://github.com/baileyrd/naner/issues?q=is%3Aissue+is%3Aopen+label%3Aenhancement

## Issue Types

### Bug Reports
Use the **Bug Report** template when:
- Something doesn't work as expected
- You encounter an error or crash
- Configuration or commands produce incorrect results
- Documentation doesn't match actual behavior

**Template:** `.github/ISSUE_TEMPLATE/bug_report.yml`

### Feature Requests
Use the **Feature Request** template when:
- You want to suggest a new feature
- You have ideas for improving existing functionality
- You need integration with a new tool or service

**Template:** `.github/ISSUE_TEMPLATE/feature_request.yml`

### Testing Issues
Use the **Testing Issue** template when:
- You find issues during systematic testing
- Automated tests are failing
- You're doing regression testing after changes
- You're validating a release candidate

**Template:** `.github/ISSUE_TEMPLATE/testing_issue.yml`

## Issue Workflow

### 1. Before Creating an Issue

- **Search existing issues:** Check if someone already reported it
- **Run diagnostics:** Execute `naner --diagnose` to gather system info
- **Check documentation:** Review relevant docs to ensure it's not expected behavior
- **Try minimal config:** Test with minimal configuration to isolate the issue

### 2. Creating an Issue

1. Go to https://github.com/baileyrd/naner/issues/new/choose
2. Select the appropriate template
3. Fill out all required fields
4. Include diagnostic output from `naner --diagnose`
5. Add relevant labels if you have permissions

### 3. Issue Labels

**Type Labels:**
- `bug` - Something isn't working
- `enhancement` - New feature or improvement
- `testing` - Issues found during testing
- `documentation` - Documentation improvements

**Priority Labels:**
- `critical` - Crashes, data loss, security issues
- `high` - Major functionality broken
- `medium` - Important but not blocking
- `low` - Minor issues, cosmetic problems

**Status Labels:**
- `needs-triage` - Needs review and prioritization (auto-assigned)
- `confirmed` - Issue has been verified
- `in-progress` - Someone is working on it
- `blocked` - Cannot proceed due to dependencies
- `duplicate` - Already reported elsewhere
- `wontfix` - Not planned to be fixed

**Component Labels:**
- `config` - Configuration system
- `launcher` - Terminal launching
- `cli` - Command-line interface
- `diagnostics` - Error handling and diagnostics
- `portable-tools` - Portable tool integration
- `build` - Build system issues

### 4. Issue Prioritization

Issues are prioritized based on:

1. **Critical:** Security, crashes, data loss
2. **High:** Core functionality broken, blocking workflows
3. **Medium:** Features work but have problems
4. **Low:** Edge cases, cosmetic issues, enhancements

## Local Issue Tracking

For quick local tracking during development, you can also maintain a simple list here:

### Current Known Issues (Delete this section once moved to GitHub)

**Example format:**
```markdown
#### Issue: Terminal fails to launch with custom profile
- **Type:** Bug
- **Severity:** High
- **Found:** 2026-01-09 during testing
- **Steps:** Run `naner custom-profile`, receive error
- **Diagnostic:** Error in TerminalLauncher.cs line 123
- **Status:** Not yet filed
- **GitHub Issue:** TBD
```

---

## Testing Checklist

When testing Naner for issues, verify:

- [ ] `naner --version` shows correct version
- [ ] `naner --help` displays complete help text
- [ ] `naner --diagnose` runs and shows system info
- [ ] `naner init` creates directory structure
- [ ] `naner init --minimal` creates minimal config
- [ ] `naner` launches default terminal profile
- [ ] Custom profiles launch correctly
- [ ] PATH environment is set correctly
- [ ] NANER_ROOT detection works from various locations
- [ ] Configuration loading handles missing files gracefully
- [ ] Error messages are clear and actionable
- [ ] Exit codes are correct (0 for success, 1 for failure)

## Reporting Test Results

After testing, create issues for any failures using the appropriate template. Group related issues together if they share a root cause.

## For Maintainers

### Triaging Issues

1. **Verify** the issue is reproducible
2. **Classify** with appropriate labels
3. **Prioritize** based on severity and impact
4. **Assign** if someone is working on it
5. **Link** to related issues or PRs

### Closing Issues

Close issues when:
- Fixed and merged to main branch
- Duplicate of another issue (link to original)
- Cannot reproduce and no additional info provided
- Won't fix with clear explanation
- Resolved by external changes

## Tips for Good Issue Reports

1. **Be specific:** "Terminal doesn't launch" vs "PowerShell profile fails with PATH error"
2. **Include context:** Version, OS, configuration
3. **Show evidence:** Error messages, diagnostic output, screenshots
4. **One issue per report:** Don't combine multiple unrelated problems
5. **Follow up:** Respond to questions and provide requested info

## Getting Help

If you're not sure whether something is a bug or how to report it:
1. Check the [Documentation](docs/README.md)
2. Run `naner --diagnose` for troubleshooting
3. Ask in [GitHub Discussions](https://github.com/baileyrd/naner/discussions)
4. When in doubt, file an issue - we can triage it
