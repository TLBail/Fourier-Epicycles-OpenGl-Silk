﻿using Silk.NET.OpenGL;
using System.Numerics;


public class Shader : IDisposable
{
    public uint _handle { get; private set; }
    private GL _gl;
    private bool disposed = false;
    
    
    public Shader(GL gl, string vertexPath, string geometryPath, string fragmentPath) {
        _gl = gl;

        uint vertex = LoadShader(ShaderType.VertexShader, vertexPath);
        uint fragment = LoadShader(ShaderType.FragmentShader, fragmentPath);
        uint geometry = LoadShader(ShaderType.GeometryShader, geometryPath);
        _handle = _gl.CreateProgram();
        _gl.AttachShader(_handle, vertex);
        _gl.AttachShader(_handle, fragment);
        _gl.AttachShader(_handle, geometry);
        _gl.LinkProgram(_handle);
        _gl.GetProgram(_handle, GLEnum.LinkStatus, out var status);
        if (status == 0) {
            throw new Exception($"Program failed to link with error: {_gl.GetProgramInfoLog(_handle)}");
        }

        _gl.DetachShader(_handle, vertex);
        _gl.DetachShader(_handle, geometry);
        _gl.DetachShader(_handle, fragment);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);


        uint uniformMatrices = getUniformBlockIndex("World");
        _gl.UniformBlockBinding(_handle, uniformMatrices, 0);

    }
    
    public Shader(GL gl, string vertexPath, string fragmentPath) {
        _gl = gl;

        uint vertex = LoadShader(ShaderType.VertexShader, vertexPath);
        uint fragment = LoadShader(ShaderType.FragmentShader, fragmentPath);
        _handle = _gl.CreateProgram();
        _gl.AttachShader(_handle, vertex);
        _gl.AttachShader(_handle, fragment);
        _gl.LinkProgram(_handle);
        _gl.GetProgram(_handle, GLEnum.LinkStatus, out var status);
        if (status == 0) {
            throw new Exception($"Program failed to link with error: {_gl.GetProgramInfoLog(_handle)}");
        }

        _gl.DetachShader(_handle, vertex);
        _gl.DetachShader(_handle, fragment);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);


        uint uniformMatrices = getUniformBlockIndex("Matrices");
        _gl.UniformBlockBinding(_handle, uniformMatrices, 0);
    }

    public void Use() {
        _gl.UseProgram(_handle);
    }

    public uint getUniformBlockIndex(string name) {
        return _gl.GetUniformBlockIndex(_handle, name);
    }

    public void SetUniform(string name, int value) {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1) {
            throw new Exception($"{name} uniform not found on shader.");
        }

        _gl.Uniform1(location, value);
    }

    public unsafe void SetUniform(string name, Matrix4x4 value) {
        //A new overload has been created for setting a uniform so we can use the transform in our shader.
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1) {
            throw new Exception($"{name} uniform not found on shader.");
        }

        _gl.UniformMatrix4(location, 1, false, (float*)&value);
    }

    public void SetUniform(string name, float value) {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1) {
            throw new Exception($"{name} uniform not found on shader.");
        }

        _gl.Uniform1(location, value);
    }

    public void SetUniform(string name, Vector3 value) {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1) {
            throw new Exception($"{name} uniform not found on shader.");
        }

        _gl.Uniform3(location, value.X, value.Y, value.Z);
    }

    ~Shader() {
        Dispose(false);
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing) {
        if (!disposed) {
            if (disposing) {
                _gl.DeleteProgram(_handle);
            }

            disposed = true;
        }
    }

    private uint LoadShader(ShaderType type, string path) {
        string src = File.ReadAllText(path);
        uint handle = _gl.CreateShader(type);
        _gl.ShaderSource(handle, src);
        _gl.CompileShader(handle);
        string infoLog = _gl.GetShaderInfoLog(handle);
        if (!string.IsNullOrWhiteSpace(infoLog)) {
            throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
        }

        return handle;
    }
}