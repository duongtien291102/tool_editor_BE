using System.Linq.Expressions;

namespace AiVideoStudio.Domain.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
}
