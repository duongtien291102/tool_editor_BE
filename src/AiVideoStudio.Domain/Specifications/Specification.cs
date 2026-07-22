using System.Linq.Expressions;

namespace AiVideoStudio.Domain.Specifications;

public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public Expression<Func<T, bool>> Criteria => ToExpression();
}
