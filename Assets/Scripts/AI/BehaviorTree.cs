using System;
using System.Collections.Generic;
using UnityEngine;

namespace Albia.AI
{
    /// <summary>
    /// Simple behavior tree system for complex AI
    /// MVP: Selector and Sequence nodes
    /// Full: Complex decorators, blackboard
    /// </summary>
    public abstract class BTNode
    {
        public enum Status { Success, Failure, Running }
        public abstract Status Execute();
    }

    public class BTSelector : BTNode
    {
        private List<BTNode> children = new List<BTNode>();
        
        public void AddChild(BTNode node) => children.Add(node);
        
        public override Status Execute()
        {
            foreach (var child in children)
            {
                var status = child.Execute();
                if (status != Status.Failure) return status;
            }
            return Status.Failure;
        }
    }

    public class BTSequence : BTNode
    {
        private List<BTNode> children = new List<BTNode>();
        
        public void AddChild(BTNode node) => children.Add(node);
        
        public override Status Execute()
        {
            foreach (var child in children)
            {
                var status = child.Execute();
                if (status == Status.Failure) return status;
                if (status == Status.Running) return status;
            }
            return Status.Success;
        }
    }

    public class BTAction : BTNode
    {
        private Func<Status> action;
        
        public BTAction(Func<Status> a) => action = a;
        
        public override Status Execute() => action();
    }

    public class BTCondition : BTNode
    {
        private Func<bool> condition;
        
        public BTCondition(Func<bool> c) => condition = c;
        
        public override Status Execute() => condition() ? Status.Success : Status.Failure;
    }
}