# Blackbird.io Utilities

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

This app provides generic utility actions that can come in useful in any bird. It includes utilities for dates and files.

## Connecting

1. Navigate to apps and search for Utilities. If you cannot find Zendesk then click _Add App_ in the top right corner, select Zendesk and add the app to your Blackbird environment.
2. Click _Add Connection_.
3. Name your connection for future reference e.g. 'My utilities'.
4. Click _Connect_.

![1700129917822](image/README/1700129917822.png)

## Actions

### Dates

- **Generate date** generates the date and time at the moment of _execution_. Optionally, you can add or subtract days, hours and minutes. Useful for setting deadlines in the future or for generating time intervals for querying data.
- **Format date** takes a date and formats it into a human readable string. The selectable format documentation can be found [here](https://ss64.com/ps/syntax-dateformats.html). Optionally a culture (locale) can be added as well.

### Files

- **Get file name** returns the name of a file without extension.
- **Change file name** takes a file and a text input. The filename (without extension) is renamed and the file is returned as output.
- **Sanitize file name** removes any defined characters from a file name (without extension).

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
