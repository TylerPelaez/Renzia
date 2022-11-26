using System;

public class State<T>
{
    public string Name { get; set; }
    public T ID { get; private set; }

    public event EventHandler OnEnter;
    public event EventHandler OnExit;
    public event EventHandler OnUpdate;
    public event EventHandler OnFixedUpdate;

    public State(T id)
    {
        ID = id;
    }
    public State(T id, string name) : this(id)
    {
        Name = name;
    }

    public State(T id,
        EventHandler onEnter,
        EventHandler onExit = null,
        EventHandler onUpdate = null,
        EventHandler onFixedUpdate = null) : this(id)
    {
        OnEnter += onEnter;
        OnExit += onExit;
        OnUpdate += onUpdate;
        OnFixedUpdate += onFixedUpdate;
    }

    public State(T id,
        string name,
        EventHandler onEnter,
        EventHandler onExit = null,
        EventHandler onUpdate = null,
        EventHandler onFixedUpdate = null) : this(id, name)
    {
        OnEnter += onEnter;
        OnExit += onExit;
        OnUpdate += onUpdate;
        OnFixedUpdate += onFixedUpdate;
    }

    virtual public void Enter()
    {
        OnEnter?.Invoke(this, EventArgs.Empty);
    }
    virtual public void Exit()
    {
        OnExit?.Invoke(this, EventArgs.Empty);
    }
    virtual public void Update()
    {
        OnUpdate?.Invoke(this, EventArgs.Empty);
    }
    virtual public void FixedUpdate()
    {
        OnFixedUpdate?.Invoke(this, EventArgs.Empty);
    }
}
