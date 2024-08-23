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

- **Generate date** generates the date and time at the moment of _execution_. Optionally, you can add or subtract days, hours and minutes. Useful for setting deadlines in the future or for generating time intervals for querying data. Optionally, you can also add your own value for date. The new generated date will be relative to this date.
- **Format date** takes a date and formats it into a human readable string. The selectable format documentation can be found [here](https://ss64.com/ps/syntax-dateformats.html). Optionally a culture (locale) can be added as well.

### Files

- **Get file character count** returns number of characters in the file (supported file types: doc, docx, txt, pdf).
- **Get file word count** returns number of words in the file (supported file types: doc, docx, txt, pdf).
- **Get file name information** returns the name of a file, with or without extension, and the extension.
- **Change file name** takes a file and a text input. The filename (without extension) is renamed and the file is returned as output.
- **Sanitize file name** removes any defined characters from a file name (without extension).
- **Get file size** returns the size of a file in bytes.

### XML Files

- **Bump version string** Bump version string.
- **Get XML file property** Get XML file property.
- **Change XML file property** Change XML file property.

### Texts

- **Count characters in text** returns number of chracters in text.
- **Count words in text** returns number of words in text.
- **Sanitize text** removes any defined characters from a text.
- **Extract using Regex** returns the first match from the provided text using a Regular Expression as input.
- **Extract many using Regex** returns all matches from the provided text using a Regular Expression as input.
- **Replace using Regex** use Regular Expressions to search and replace within text
- **Convert text to document** Converts text to txt, doc or docx document.
- **Convert document to text** Extracts document's text. Document must be in docx/doc, pdf or txt format.

### Arrays

- **Array contains** checks if an array contains a certain entry.
- **Deduplicate Array** Returns only unique elements.
- **Array count** counts the number of elements in an array.
- **Remove entry from array** returns the array without the specified entry.
- **Get first entry from array** returns the first element in the array.
- **Get last entry from array** returns the last element in the array.

### Numbers

- **Generate Range** Generate a range by providing start and end numbers.

### Scraping

- **Extract web page content** Get raw and unformatted content from a URL as text.
- **Extract HTML content** Get raw and unformatted content from an HTML file.

### Context

- **Get flight context** allows you to get context data from the flight. This includes Flight ID and URL, Bird ID and name and Nest ID and name.

## Events

### Rss

- **On RSS feed changed** triggers when specified RSS feed received new updates.

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
