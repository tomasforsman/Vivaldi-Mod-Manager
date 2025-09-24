# Security Policy

## Supported Versions

We actively maintain security updates for the following versions of Vivaldi Mod Manager:

| Version | Supported          |
| ------- | ------------------ |
| 1.3.x   | :white_check_mark: |
| < 1.3   | :x:                |

## Reporting a Vulnerability

We take security seriously and appreciate your efforts to responsibly disclose security vulnerabilities.

### How to Report

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report security vulnerabilities by email to: **[maintainer email to be added]**

Include the following information in your report:

- A clear description of the vulnerability
- Steps to reproduce the issue
- Potential impact of the vulnerability
- Any suggested fixes or mitigations
- Your contact information (optional, but helpful for follow-up)

### What to Expect

1. **Acknowledgment**: We will acknowledge receipt of your vulnerability report within 48 hours.

2. **Initial Assessment**: We will conduct an initial assessment within 7 days and provide an estimated timeline for resolution.

3. **Updates**: We will keep you informed of our progress throughout the investigation and resolution process.

4. **Resolution**: We aim to resolve critical security issues within 30 days of initial report.

5. **Disclosure**: After the vulnerability is resolved, we will work with you on an appropriate disclosure timeline.

## Security Considerations for Users

### Installation Security

- Always download Vivaldi Mod Manager from official sources (GitHub releases)
- Verify file hashes when available
- Use Windows Defender or equivalent antivirus software

### Mod Security

- Only install JavaScript mods from trusted sources
- Review mod code before installation when possible
- Be aware that mods run with browser privileges and can access web content
- Use Safe Mode to disable all mods if you suspect issues

### System Security

- Keep Windows and .NET runtime up to date
- Run Vivaldi Mod Manager with minimal necessary privileges
- Regular backups are automatically created, but consider additional backups of important configurations

### Privacy Considerations

- Mods may have access to web page content and user interactions
- Review mod permissions and functionality before enabling
- Some mods may collect or transmit data - review their privacy policies

## Responsible Disclosure

We believe in responsible disclosure and will:

- Work with security researchers to understand and fix vulnerabilities
- Provide credit to researchers who report vulnerabilities (unless they prefer to remain anonymous)
- Maintain open communication throughout the process
- Provide security advisories for resolved vulnerabilities when appropriate

## Security Best Practices for Contributors

If you're contributing to Vivaldi Mod Manager:

- Never commit sensitive information (passwords, tokens, keys) to the repository
- Use secure coding practices for input validation and sanitization
- Consider security implications of any file system or registry operations
- Test security-related changes thoroughly
- Follow the principle of least privilege in code design

## Questions

If you have questions about this security policy, please open a discussion on GitHub or contact the maintainers.