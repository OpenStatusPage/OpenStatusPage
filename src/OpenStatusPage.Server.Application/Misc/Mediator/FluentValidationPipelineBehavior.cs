using FluentValidation;
using FluentValidation.Results;

using MediatR;
using OpenStatusPage.Server.Application.Misc.Exceptions;

namespace OpenStatusPage.Server.Application.Misc.Mediator;

public class FluentValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public FluentValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        var validationResults = new List<ValidationResult>();

        try
        {
            foreach (var validator in _validators)
            {
                var result = await validator.ValidateAsync(request, cancellationToken);

                validationResults.Add(result);
            }
        }
        catch
        {
            throw new FinalFailureException("Request failed.", new ValidationException("Could not validate the request."));
        }

        var validationFailures = validationResults
            .SelectMany(validationResult => validationResult.Errors)
            .Where(validationFailure => validationFailure != null)
            .ToList();

        if (validationFailures.Any())
        {
            throw new FinalFailureException("Request failed.", new ValidationException(validationFailures));
        }

        return await next();
    }
}
