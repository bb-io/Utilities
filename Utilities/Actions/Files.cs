﻿using Apps.Utilities.Models.Dates;
using Apps.Utilities.Models.Files;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.Actions
{
    [ActionList]
    public class Files : BaseInvocable
    {
        public Files(InvocationContext context) : base(context) { }

        [Action("Get file name", Description = "Returns the name of a file (without extension).")]
        public NameResponse GetFileName([ActionParameter] FileRequest file)
        {
            return new NameResponse { Name = Path.GetFileNameWithoutExtension(file.File.Name) };
        }

        [Action("Change file name", Description = "Rename a file (without extension).")]
        public FileResponse ChangeFileName([ActionParameter] FileRequest file, [ActionParameter] RenameRequest input)
        {
            var extension = Path.GetExtension(file.File.Name);
            file.File.Name = input.Name + extension;
            return new FileResponse { File = file.File };
        }

        [Action("Sanitize file name", Description = "Remove any defined characters from a file name (without extension).")]
        public FileResponse SanitizeFileName([ActionParameter] FileRequest file, [ActionParameter] SanitizeRequest input)
        {
            var extension = Path.GetExtension(file.File.Name);
            var newName = file.File.Name;
            foreach (string filteredCharacter in input.FilterCharacters)
            {
                newName = newName.Replace(filteredCharacter, string.Empty);
            }
            file.File.Name = newName + extension;
            return new FileResponse { File = file.File };
        }
    }
}
