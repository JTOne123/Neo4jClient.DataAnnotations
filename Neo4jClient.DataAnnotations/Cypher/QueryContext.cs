﻿using Neo4jClient.Cypher;
using Neo4jClient.DataAnnotations.Serialization;
using Neo4jClient.Serialization;
using System;
using Neo4jClient.DataAnnotations.Utils;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;

namespace Neo4jClient.DataAnnotations.Cypher
{
    public class QueryContext : IHaveAnnotationsContext, IHaveEntityService
    {
        //public ICypherFluentQuery Query { get; set; }

        public IGraphClient Client { get; set; }
        public ISerializer ISerializer { get; set; }
        public EntityResolver Resolver => AnnotationsContext.EntityResolver; //{ get; set; }
        public EntityConverter Converter => AnnotationsContext.EntityConverter; //{ get; set; }
        public Func<object, string> SerializeCallback { get; set; }

        private AnnotationsContext annotationsContext;
        public AnnotationsContext AnnotationsContext
        {
            get
            {
                if (annotationsContext == null)
                {
                    annotationsContext = Client?.GetAnnotationsContext()
                        ?? Resolver?.AnnotationsContext
                        ?? Converter?.AnnotationsContext;
                }
                return annotationsContext;
            }
            set
            {
                annotationsContext = value;
            }
        }

        public EntityService EntityService => AnnotationsContext?.EntityService;

        /// <summary>
        /// Note that this query writer is useless against subsequent <see cref="ICypherFluentQuery"/> calls when the query object changes
        /// </summary>
        public QueryWriterWrapper CurrentQueryWriter { get; set; }
        /// <summary>
        /// Note that this build strategy might be is useless against subsequent <see cref="ICypherFluentQuery"/> calls when the query object changes
        /// </summary>
        public PropertiesBuildStrategy? CurrentBuildStrategy { get; set; }

        public static Func<ICypherFluentQuery, QueryWriterWrapper> QueryWriterGetter { get; } = (q) =>
        {
            QueryWriter qw = Defaults.QueryWriterInfo.GetValue(q) as QueryWriter;
            return qw != null ? new QueryWriterWrapper(qw, q.GetAnnotationsContext()) : null;
        };

        public static Func<ICypherFluentQuery, PropertiesBuildStrategy?> BuildStrategyGetter { get; } = (q) =>
        {
            QueryWriter qw = Defaults.QueryWriterInfo.GetValue(q) as QueryWriter;
            var parameters = Defaults.QueryWriterParamsInfo.GetValue(qw) as IDictionary<string, object>;

            if (parameters != null && parameters.TryGetValue(Defaults.QueryBuildStrategyKey, out var buildStrategy))
                return buildStrategy as PropertiesBuildStrategy?;

            return null;
        };

        private Dictionary<Expression, (Expression NewNode, string Build)> funcsCachedBuilds;
        public Dictionary<Expression, (Expression NewNode, string Build)> FuncsCachedBuilds =>
            funcsCachedBuilds ?? (funcsCachedBuilds = new Dictionary<Expression, (Expression NewNode, string Build)>());

        private Dictionary<Expression, JToken> funcsCachedJTokens;
        public Dictionary<Expression, JToken> FuncsCachedJTokens =>
            funcsCachedJTokens ?? (funcsCachedJTokens = new Dictionary<Expression, JToken>());
    }
}
