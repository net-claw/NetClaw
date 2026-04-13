using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetClaw.AspNetCore.Extensions.ErrorHandlers
{
    public class ProblemResultCollection : Dictionary<string, ICollection<ProblemResult>>
    {
        public ProblemResultCollection()
        {
        }

        public ProblemResultCollection([NotNull] IEnumerable<GenericValidationResult> errors) => this.AddRange(errors);

        public ProblemResultCollection([NotNull] IEnumerable<ValidationResult> errors) => this.AddRange(errors);

        public void Add(GenericValidationResult result)
        {
            ProblemResult problemResult = result.ToProblemResult();
            IEnumerable<string> memberNames = result.MemberNames;
            if ((memberNames != null ? (memberNames.Any<string>() ? 1 : 0) : 0) != 0)
            {
                foreach (string memberName in result.MemberNames)
                {
                    if (this.ContainsKey(memberName))
                        this[memberName].Add(problemResult);
                    else
                        this.Add(memberName, (ICollection<ProblemResult>)new List<ProblemResult>((IEnumerable<ProblemResult>)new ProblemResult[1]
                        {
              problemResult
                        }));
                }
            }
            else if (this.ContainsKey(string.Empty))
                this[string.Empty].Add(problemResult);
            else
                this.Add(string.Empty, (ICollection<ProblemResult>)new List<ProblemResult>((IEnumerable<ProblemResult>)new ProblemResult[1]
                {
          problemResult
                }));
        }

        public void AddRange(IEnumerable<GenericValidationResult> results)
        {
            foreach (GenericValidationResult result in results)
                this.Add(result);
        }

        public void Add(ValidationResult result) => this.Add(new GenericValidationResult(result));

        public void AddRange(IEnumerable<ValidationResult> results)
        {
            foreach (ValidationResult result in results)
                this.Add(result);
        }
    }
}
