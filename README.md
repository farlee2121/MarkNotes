# Notedown

Notedown is a [set of conventions](https://spencerfarley.com/2021/03/05/reference-ready-notes/) for notes in markdown and tools that operate on the conventions.
These conventions aim to
- be intuitive as plain text
- Not interrupt flow while taking notes
- Make notes easy to skim for reference
- easily back into existing notes
- [Treat our notes as a data source](https://spencerfarley.com/2021/03/05/reference-ready-notes/)

This repository contains tools for programmatically leveraging Notedown conventions.

## Tag Extraction

```powershell
notedown extract-tags file-here.md -t "TAG:" -o output-file.md
```

Extract all content with the given tag by the following rules
- Paragraph: Extracts the full paragraph if the tag appears anywhere in the paragraph
- Paragraph+list: Extracts the paragraph and following list if there is no space between the paragraph and list
- List items: Extracts any list items containing the tag along with any sub-items (but not parent items)
- Headings/Sections: Extracts any heading containing the tag along with all content in the heading's section