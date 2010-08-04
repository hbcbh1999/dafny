//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics.Contracts;
using Bpl = Microsoft.Boogie;
using System.Text;
using Microsoft.Boogie;

namespace Microsoft.Dafny {
  public class Translator {
    [NotDelayed]
    public Translator() {
      Bpl.Program boogieProgram = ReadPrelude();
      if (boogieProgram != null) {
        sink = boogieProgram;
        predef = FindPredefinedDecls(boogieProgram);
      }
      //base();
      if (predef != null) {
        cevVariables.Add(predef.HeapVarName, predef.CevHeapName);
      }
    }
    
    // translation state
    readonly Dictionary<TopLevelDecl/*!*/,Bpl.Constant/*!*/>/*!*/ classes = new Dictionary<TopLevelDecl/*!*/,Bpl.Constant/*!*/>();
    readonly Dictionary<Field/*!*/,Bpl.Constant/*!*/>/*!*/ fields = new Dictionary<Field/*!*/,Bpl.Constant/*!*/>();

    // Machinery for providing information to the Counterexample Visualizer
    readonly Dictionary<string/*!*/,int>/*!*/ cevFilenames = new Dictionary<string/*!*/,int>();
    readonly Dictionary<IToken/*!*/,Bpl.Constant/*!*/>/*!*/ cevLocations = new Dictionary<IToken/*!*/,Bpl.Constant/*!*/>();
    readonly Dictionary<string/*!*/,Bpl.Constant/*!*/>/*!*/ cevVariables = new Dictionary<string/*!*/,Bpl.Constant/*!*/>();

    [ContractInvariantMethod]
void ObjectInvariant() 
{
    Contract.Invariant(cce.NonNullElements(classes));
      Contract.Invariant(cce.NonNullElements(fields));
      Contract.Invariant(cce.NonNullElements(cevFilenames));
      Contract.Invariant(cce.NonNullElements(cevLocations));
      Contract.Invariant(cce.NonNullElements(cevVariables));
}


    Bpl.Expr CevLocation(IToken tok)
  {
      Contract.Requires(tok != null);
    Contract.Requires( predef != null && sink != null);
      Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

    
      Bpl.Constant c;
      if (cevLocations.TryGetValue(tok, out c)) {
        Contract.Assert( c != null);
      } else {
        int fileId;
        string filename = "#file^" + (tok.filename == null ? ".dfy" : tok.filename);
        if (!cevFilenames.TryGetValue(filename, out fileId)) {
          fileId = cevFilenames.Count;
          cevFilenames.Add(filename, fileId);
          Bpl.Constant fn = new Bpl.Constant(tok, new Bpl.TypedIdent(tok, Sanitize(filename), predef.CevTokenType), true);
          sink.TopLevelDeclarations.Add(fn);
          sink.TopLevelDeclarations.Add(new Bpl.Axiom(tok, FunctionCall(tok, "$file_name_is", Bpl.Type.Bool, Bpl.Expr.Literal(fileId), new Bpl.IdentifierExpr(tok, fn))));
        }
        c = new Bpl.Constant(tok, new Bpl.TypedIdent(tok, string.Format("#tok${0}^{1}.{2}", fileId, tok.line, tok.col), predef.CevTokenType), true);
        cevLocations.Add(tok, c);
        sink.TopLevelDeclarations.Add(c);
      }
      return new Bpl.IdentifierExpr(tok, c);
    }
    static string Sanitize(string s) {
      Contract.Requires(s != null);
      Contract.Ensures(Contract.Result<string>() != null);

      StringBuilder sb = new StringBuilder();
      foreach (char ch in s) {
        if (char.IsLetterOrDigit(ch) || ch == '#' || ch == '^' || ch == '$' || ch == '.' || ch == '@') {
          sb.Append(ch);
        } else {
          sb.Append(string.Format("%{0:X4}", (int)ch));
        }
      }
      return sb.ToString();
    }
    Bpl.Expr CevVariable(Bpl.IToken tok, string name)
    {
      Contract.Requires(tok != null);
      Contract.Requires(name != null);
      Contract.Requires( predef != null && sink != null);
      Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

      
      Bpl.Constant c;
      if (cevVariables.TryGetValue(name, out c)) {
        Contract.Assert( c != null);
      } else {
        c = new Bpl.Constant(tok, new Bpl.TypedIdent(tok, string.Format("#loc.{0}", name), predef.CevTokenType), true);
        cevVariables.Add(name, c);
        sink.TopLevelDeclarations.Add(c);
      }
      return new Bpl.IdentifierExpr(tok, c);
    }
    
    readonly Bpl.Program sink;
    readonly PredefinedDecls predef;

    internal class PredefinedDecls {
      public readonly Bpl.Type RefType;
      public readonly Bpl.Type BoxType;
      public readonly Bpl.Type CevTokenType;
      public readonly Bpl.Type CevVariableKind;
      public readonly Bpl.Type CevEventType;
      private readonly Bpl.TypeSynonymDecl setTypeCtor;
      private readonly Bpl.TypeCtorDecl seqTypeCtor;
      readonly Bpl.TypeCtorDecl fieldName;
      public readonly Bpl.Type HeapType;
      public readonly string HeapVarName;
      public readonly Bpl.Constant CevHeapName;
      public readonly Bpl.Type ClassNameType;
      public readonly Bpl.Type FieldCategoryType;
      public readonly Bpl.Type DatatypeType;
      public readonly Bpl.Type DtCtorId;
      public readonly Bpl.Expr Null;
      private readonly Bpl.Constant allocField;
      [ContractInvariantMethod]
      void ObjectInvariant() 
{
    Contract.Invariant(RefType!=null);
        Contract.Invariant(BoxType != null);
        Contract.Invariant(CevTokenType != null);
        Contract.Invariant(CevVariableKind != null);
        Contract.Invariant(CevEventType != null);
        Contract.Invariant(setTypeCtor != null);
        Contract.Invariant(seqTypeCtor != null);
        Contract.Invariant(fieldName != null);
        Contract.Invariant(HeapType != null);
        Contract.Invariant(HeapVarName != null);
        Contract.Invariant(CevEventType != null);
        Contract.Invariant(ClassNameType != null);
        Contract.Invariant(FieldCategoryType != null);
        Contract.Invariant(DatatypeType != null);
        Contract.Invariant(DtCtorId != null);
        Contract.Invariant(Null != null);
        Contract.Invariant(allocField != null);
}


      public Bpl.Type SetType(IToken tok, Bpl.Type ty) {
        Contract.Requires(tok != null);
        Contract.Requires(ty != null);
        Contract.Ensures(Contract.Result<Bpl.Type>() != null);

        return new Bpl.TypeSynonymAnnotation(Token.NoToken, setTypeCtor, new Bpl.TypeSeq(ty));
      }
      
      public Bpl.Type SeqType(IToken tok, Bpl.Type ty) {
        Contract.Requires(tok != null);
        Contract.Requires(ty != null);
        Contract.Ensures(Contract.Result<Bpl.Type>() != null);
        return new Bpl.CtorType(Token.NoToken, seqTypeCtor, new Bpl.TypeSeq(ty));
      }
      
      public Bpl.Type FieldName(IToken tok, Bpl.Type ty) {
        Contract.Requires(tok != null);
        Contract.Requires(ty != null);
        Contract.Ensures(Contract.Result<Bpl.Type>() != null);

        return new Bpl.CtorType(tok, fieldName, new Bpl.TypeSeq(ty));
      }
      
      public Bpl.IdentifierExpr Alloc(IToken tok) {
        Contract.Requires(tok != null);
        Contract.Ensures(Contract.Result<Bpl.IdentifierExpr>() != null);

        return new Bpl.IdentifierExpr(tok, allocField);
      }

      public PredefinedDecls(Bpl.TypeCtorDecl refType, Bpl.TypeCtorDecl boxType, Bpl.TypeCtorDecl cevTokenType, 
                             Bpl.TypeCtorDecl cevVariableKind, Bpl.TypeCtorDecl cevEventType,
                             Bpl.TypeSynonymDecl setTypeCtor, Bpl.TypeCtorDecl seqTypeCtor, Bpl.TypeCtorDecl fieldNameType,
                             Bpl.GlobalVariable heap, Bpl.TypeCtorDecl classNameType, Bpl.TypeCtorDecl fieldCategoryType,
                             Bpl.TypeCtorDecl datatypeType, Bpl.TypeCtorDecl dtCtorId,
                             Bpl.Constant allocField, Bpl.Constant cevHeapNameConst) {
        #region Non-null preconditions on parameters
        Contract.Requires(refType != null);
        Contract.Requires(boxType != null);
        Contract.Requires(cevTokenType != null);
        Contract.Requires(cevVariableKind != null);
        Contract.Requires(cevEventType != null);
        Contract.Requires(setTypeCtor != null);
        Contract.Requires(seqTypeCtor != null);
        Contract.Requires(fieldNameType != null);
        Contract.Requires(heap != null);
        Contract.Requires(classNameType != null);
        Contract.Requires(fieldCategoryType != null);
        Contract.Requires(datatypeType != null);
        Contract.Requires(dtCtorId != null);
        Contract.Requires(allocField != null);
        Contract.Requires(cevHeapNameConst != null);
        #endregion

        Bpl.CtorType refT = new Bpl.CtorType(Token.NoToken, refType, new Bpl.TypeSeq());
        this.RefType = refT;
        this.BoxType = new Bpl.CtorType(Token.NoToken, boxType, new Bpl.TypeSeq());
        this.CevTokenType = new Bpl.CtorType(Token.NoToken, cevTokenType, new Bpl.TypeSeq());
        this.CevVariableKind = new Bpl.CtorType(Token.NoToken, cevVariableKind, new Bpl.TypeSeq());
        this.CevEventType = new Bpl.CtorType(Token.NoToken, cevEventType, new Bpl.TypeSeq());
        this.setTypeCtor = setTypeCtor;
        this.seqTypeCtor = seqTypeCtor;
        this.fieldName = fieldNameType;
        this.HeapType = heap.TypedIdent.Type;
        this.HeapVarName = heap.Name;
        this.CevHeapName = cevHeapNameConst;
        this.ClassNameType = new Bpl.CtorType(Token.NoToken, classNameType, new Bpl.TypeSeq());
        this.FieldCategoryType = new Bpl.CtorType(Token.NoToken, fieldCategoryType, new Bpl.TypeSeq());
        this.DatatypeType = new Bpl.CtorType(Token.NoToken, datatypeType, new Bpl.TypeSeq());
        this.DtCtorId = new Bpl.CtorType(Token.NoToken, dtCtorId, new Bpl.TypeSeq());
        this.allocField = allocField;
        this.Null = new Bpl.IdentifierExpr(Token.NoToken, "null", refT);
      }
    }
    
    static PredefinedDecls FindPredefinedDecls(Bpl.Program prog) {
      Contract.Requires(prog != null);
      if (prog.Resolve() != 0) {
        Console.WriteLine("Error: resolution errors encountered in Dafny prelude");
        return null;
      }
      
      Bpl.TypeCtorDecl refType = null;
      Bpl.TypeCtorDecl cevTokenType = null;
      Bpl.TypeCtorDecl cevVariableKind = null;
      Bpl.TypeCtorDecl cevEventType = null;
      Bpl.TypeSynonymDecl setTypeCtor = null;
      Bpl.TypeCtorDecl seqTypeCtor = null;
      Bpl.TypeCtorDecl fieldNameType = null;
      Bpl.TypeCtorDecl classNameType = null;
      Bpl.TypeCtorDecl fieldCategoryType = null;
      Bpl.TypeCtorDecl datatypeType = null;
      Bpl.TypeCtorDecl dtCtorId = null;
      Bpl.TypeCtorDecl boxType = null;
      Bpl.GlobalVariable heap = null;
      Bpl.Constant allocField = null;
      Bpl.Constant cevHeapNameConst = null;
      foreach (Bpl.Declaration d in prog.TopLevelDeclarations) {
        if (d is Bpl.TypeCtorDecl) {
          Bpl.TypeCtorDecl dt = (Bpl.TypeCtorDecl)d;
          if (dt.Name == "Seq") {
            seqTypeCtor = dt;
          } else if (dt.Name == "Field") {
            fieldNameType = dt;
          } else if (dt.Name == "ClassName") {
            classNameType = dt;
          } else if (dt.Name == "FieldCategory") {
            fieldCategoryType = dt;
          } else if (dt.Name == "DatatypeType") {
            datatypeType = dt;
          } else if (dt.Name == "DtCtorId") {
            dtCtorId = dt;
          } else if (dt.Name == "ref") {
            refType = dt;
          } else if (dt.Name == "BoxType") {
            boxType = dt;
          } else if (dt.Name == "$token") {
            cevTokenType = dt;
          } else if (dt.Name == "var_locglob") {
            cevVariableKind = dt;
          } else if (dt.Name == "cf_event") {
            cevEventType = dt;
          }
        } else if (d is Bpl.TypeSynonymDecl) {
          Bpl.TypeSynonymDecl dt = (Bpl.TypeSynonymDecl)d;
          if (dt.Name == "Set") {
            setTypeCtor = dt;
          }
        } else if (d is Bpl.Constant) {
          Bpl.Constant c = (Bpl.Constant)d;
          if (c.Name == "alloc") {
            allocField = c;
          } else if (c.Name == "#loc.$Heap") {
            cevHeapNameConst = c;
          }
        } else if (d is Bpl.GlobalVariable) {
          Bpl.GlobalVariable v = (Bpl.GlobalVariable)d;
          if (v.Name == "$Heap") {
            heap = v;
          }
        }
      }
      if (seqTypeCtor == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of type Seq");
      } else if (setTypeCtor == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of type Set");
      } else if (fieldNameType == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of type Field");
      } else if (classNameType == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of type ClassName");
      } else if (fieldCategoryType == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of type FieldCategory");
      } else if (datatypeType == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of type DatatypeType");
      } else if (dtCtorId == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of type DtCtorId");
      } else if (refType == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of type ref");
      } else if (boxType == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of type BoxType");
      } else if (cevTokenType == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of type $token");
      } else if (cevVariableKind == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of type var_locglob");
      } else if (cevEventType == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of type cf_event");
      } else if (heap == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of $Heap");
      } else if (allocField == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of constant alloc");
      } else if (cevHeapNameConst == null) {
        Console.WriteLine("Error: Dafny prelude is missing declaration of constant #loc.$Heap");
      } else {
        return new PredefinedDecls(refType, boxType, cevTokenType, cevVariableKind, cevEventType,
                                   setTypeCtor, seqTypeCtor, fieldNameType, heap, classNameType, fieldCategoryType, datatypeType, dtCtorId,
                                   allocField, cevHeapNameConst);
      }
      return null;
    }
    
    static Bpl.Program ReadPrelude() {
      //using (System.IO.Stream stream = cce.NonNull( System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("DafnyPrelude.bpl")) // Use this once Spec#/VSIP supports designating a non-.resx project item as an embedded resource
      string codebase = cce.NonNull( System.IO.Path.GetDirectoryName(cce.NonNull(System.Reflection.Assembly.GetExecutingAssembly().Location)));
      string preludePath = System.IO.Path.Combine(codebase, "DafnyPrelude.bpl");
      
      Bpl.Program prelude;
      int errorCount = Bpl.Parser.Parse(preludePath, null, out prelude);
      if (prelude == null || errorCount > 0) {
        return null;
      } else {
        return prelude;
      }
/*      
      List<string!> defines = new List<string!>();
      using (System.IO.Stream stream = new System.IO.FileStream(preludePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
      {
        BoogiePL.Buffer.Fill(new System.IO.StreamReader(stream), defines);
        //BoogiePL.Scanner.Init("<DafnyPrelude.bpl>");
        Bpl.Program prelude;
        int errorCount = BoogiePL.Parser.Parse(out prelude);
        if (prelude == null || errorCount > 0) {
          return null;
        } else {
          return prelude;
        }
      }
*/      
    }
    
    public Bpl.Program Translate(Program program) {
      Contract.Requires(program != null);
    Contract.Ensures(Contract.Result<Bpl.Program>() != null);

      if (sink == null || predef == null) {
        // something went wrong during construction, which reads the prelude; an error has
        // already been printed, so just return an empty program here (which is non-null)
        return new Bpl.Program();
      }
      foreach (ModuleDecl m in program.Modules) {
        foreach (TopLevelDecl d in m.TopLevelDecls) {
          if (d is DatatypeDecl) {
            AddDatatype((DatatypeDecl)d);
          } else {
            AddClassMembers((ClassDecl)d);
          }
        }
      }
      return sink;
    }
    
    void AddDatatype(DatatypeDecl dt)
    {
      Contract.Requires(dt != null);
      Contract.Requires( sink != null && predef != null);
    
      foreach (DatatypeCtor ctor in dt.Ctors) {
        // Add:  function #dt.ctor(paramTypes) returns (DatatypeType);
        Bpl.VariableSeq argTypes = new Bpl.VariableSeq();
        foreach (Formal arg in ctor.Formals) {
          Bpl.Variable a = new Bpl.Formal(arg.tok, new Bpl.TypedIdent(arg.tok, Bpl.TypedIdent.NoName, TrType(arg.Type)), true);
          argTypes.Add(a);
        }
        Bpl.Variable resType = new Bpl.Formal(ctor.tok, new Bpl.TypedIdent(ctor.tok, Bpl.TypedIdent.NoName, predef.DatatypeType), false);
        Bpl.Function fn = new Bpl.Function(ctor.tok, ctor.FullName, argTypes, resType);
        sink.TopLevelDeclarations.Add(fn);
        // Add:  const unique ##dt.ctor: DtCtorId;
        Bpl.Constant cid = new Bpl.Constant(ctor.tok, new Bpl.TypedIdent(ctor.tok, "#" + ctor.FullName, predef.DtCtorId), true);
        sink.TopLevelDeclarations.Add(cid);
        // Add:  axiom (forall params :: DatatypeCtorId(#dt.ctor(params)) == ##dt.ctor);
        Bpl.VariableSeq bvs;
        List<Bpl.Expr> args;
        CreateBoundVariables(ctor.Formals, out bvs, out args);
        Bpl.Expr lhs = FunctionCall(ctor.tok, ctor.FullName, predef.DatatypeType, args);
        lhs = FunctionCall(ctor.tok, BuiltinFunction.DatatypeCtorId, null, lhs);
        Bpl.Expr q = Bpl.Expr.Eq(lhs, new Bpl.IdentifierExpr(ctor.tok, cid));
        if (bvs.Length != 0) {
          q = new Bpl.ForallExpr(ctor.tok, bvs, q);
        }
        sink.TopLevelDeclarations.Add(new Bpl.Axiom(ctor.tok, q));
        // Add injectivity axioms:
        int i = 0;
        foreach (Formal arg in ctor.Formals) {
          // function ##dt.ctor#i(DatatypeType) returns (Ti);
          argTypes = new Bpl.VariableSeq();
          argTypes.Add(new Bpl.Formal(ctor.tok, new Bpl.TypedIdent(ctor.tok, Bpl.TypedIdent.NoName, predef.DatatypeType), true));
          resType = new Bpl.Formal(arg.tok, new Bpl.TypedIdent(arg.tok, Bpl.TypedIdent.NoName, TrType(arg.Type)), false);
          fn = new Bpl.Function(ctor.tok, "#" + ctor.FullName + "#" + i, argTypes, resType);
          sink.TopLevelDeclarations.Add(fn);
          // axiom (forall params :: ##dt.ctor#i(#dt.ctor(params)) == params_i);
          CreateBoundVariables(ctor.Formals, out bvs, out args);
          lhs = FunctionCall(ctor.tok, ctor.FullName, predef.DatatypeType, args);
          lhs = FunctionCall(ctor.tok, fn.Name, TrType(arg.Type), lhs);
          q = new Bpl.ForallExpr(ctor.tok, bvs, Bpl.Expr.Eq(lhs, args[i]));
          sink.TopLevelDeclarations.Add(new Bpl.Axiom(ctor.tok, q));
          if (arg.Type.IsDatatype || arg.Type.IsTypeParameter) {
            // for datatype:             axiom (forall params :: DtRank(params_i) < DtRank(#dt.ctor(params)));
            // for type-parameter type:  axiom (forall params :: DtRank(Unbox(params_i)) < DtRank(#dt.ctor(params)));
            CreateBoundVariables(ctor.Formals, out bvs, out args);
            lhs = FunctionCall(ctor.tok, BuiltinFunction.DtRank, null,
              arg.Type.IsDatatype ? args[i] : FunctionCall(ctor.tok, BuiltinFunction.Unbox, predef.DatatypeType, args[i]));
            Bpl.Expr rhs = FunctionCall(ctor.tok, ctor.FullName, predef.DatatypeType, args);
            rhs = FunctionCall(ctor.tok, BuiltinFunction.DtRank, null, rhs);
            q = new Bpl.ForallExpr(ctor.tok, bvs, Bpl.Expr.Lt(lhs, rhs));
            sink.TopLevelDeclarations.Add(new Bpl.Axiom(ctor.tok, q));
          } else if (arg.Type is SeqType) {
            // axiom (forall params, i: int :: 0 <= i && i < |arg| ==> DtRank(arg[i]) < DtRank(#dt.ctor(params)));
            // that is:
            // axiom (forall params, i: int :: 0 <= i && i < |arg| ==> DtRank(Unbox(Seq#Index(arg,i))) < DtRank(#dt.ctor(params)));
            CreateBoundVariables(ctor.Formals, out bvs, out args);
            Bpl.Variable iVar = new Bpl.BoundVariable(arg.tok, new Bpl.TypedIdent(arg.tok, "i", Bpl.Type.Int));
            bvs.Add(iVar);
            Bpl.IdentifierExpr ie = new Bpl.IdentifierExpr(arg.tok, iVar);
            Bpl.Expr ante = Bpl.Expr.And(
              Bpl.Expr.Le(Bpl.Expr.Literal(0), ie),
              Bpl.Expr.Lt(ie, FunctionCall(arg.tok, BuiltinFunction.SeqLength, null, args[i])));
            lhs = FunctionCall(ctor.tok, BuiltinFunction.DtRank, null,
              FunctionCall(arg.tok, BuiltinFunction.Unbox, predef.DatatypeType,
                FunctionCall(arg.tok, BuiltinFunction.SeqIndex, predef.DatatypeType, args[i], ie)));
            Bpl.Expr rhs = FunctionCall(ctor.tok, ctor.FullName, predef.DatatypeType, args);
            rhs = FunctionCall(ctor.tok, BuiltinFunction.DtRank, null, rhs);
            q = new Bpl.ForallExpr(ctor.tok, bvs, Bpl.Expr.Imp(ante, Bpl.Expr.Lt(lhs, rhs)));
            sink.TopLevelDeclarations.Add(new Bpl.Axiom(ctor.tok, q));
          } else if (arg.Type is SetType) {
            // axiom (forall params, d: Datatype :: arg[d] ==> DtRank(d) < DtRank(#dt.ctor(params)));
            // that is:
            // axiom (forall params, d: Datatype :: arg[Box(d)] ==> DtRank(d) < DtRank(#dt.ctor(params)));
            CreateBoundVariables(ctor.Formals, out bvs, out args);
            Bpl.Variable dVar = new Bpl.BoundVariable(arg.tok, new Bpl.TypedIdent(arg.tok, "d", predef.DatatypeType));
            bvs.Add(dVar);
            Bpl.IdentifierExpr ie = new Bpl.IdentifierExpr(arg.tok, dVar);
            Bpl.Expr ante = Bpl.Expr.SelectTok(arg.tok, args[i], FunctionCall(arg.tok, BuiltinFunction.Box, null, ie));
            lhs = FunctionCall(ctor.tok, BuiltinFunction.DtRank, null, ie);
            Bpl.Expr rhs = FunctionCall(ctor.tok, ctor.FullName, predef.DatatypeType, args);
            rhs = FunctionCall(ctor.tok, BuiltinFunction.DtRank, null, rhs);
            q = new Bpl.ForallExpr(ctor.tok, bvs, Bpl.Expr.Imp(ante, Bpl.Expr.Lt(lhs, rhs)));
            sink.TopLevelDeclarations.Add(new Bpl.Axiom(ctor.tok, q));
          }
          i++;
        }
      }
    }
    
    void CreateBoundVariables(List<Formal/*!*/>/*!*/ formals, out Bpl.VariableSeq/*!*/ bvs, out List<Bpl.Expr/*!*/>/*!*/ args)
    {
      Contract.Requires(formals != null);
      
      
      Contract.Ensures(  Contract.ValueAtReturn(out bvs).Length == Contract.ValueAtReturn(out args).Count);
      Contract.Ensures(Contract.ValueAtReturn(out bvs) != null);
      Contract.Ensures(cce.NonNullElements(Contract.ValueAtReturn(out args)));


      bvs = new Bpl.VariableSeq();
      args = new List<Bpl.Expr>();
      foreach (Formal arg in formals) {
        Contract.Assert(arg != null);
        Bpl.Variable bv = new Bpl.BoundVariable(arg.tok, new Bpl.TypedIdent(arg.tok, "a" + bvs.Length, TrType(arg.Type)));
        bvs.Add(bv);
        args.Add(new Bpl.IdentifierExpr(arg.tok, bv));
      }
    }
    
    void AddClassMembers(ClassDecl c)
    {
      Contract.Requires( sink != null && predef != null);
    
      Contract.Requires(c != null);
      sink.TopLevelDeclarations.Add(GetClass(c));
      
      foreach (MemberDecl member in c.Members) {
        if (member is Field) {
          Field f = (Field)member;
          Bpl.Constant fc = GetField(f);
          sink.TopLevelDeclarations.Add(fc);
          
          AddAllocationAxiom(f);
          
        } else if (member is Function) {
          Function f = (Function)member;
          AddFunction(f);
          if (f.Body != null) {
            if (f.Body is MatchExpr) {
              MatchExpr me = (MatchExpr)f.Body;
              Formal formal = (Formal)((IdentifierExpr)me.Source).Var;  // correctness of casts follows from what resolution checks
              foreach (MatchCaseExpr mc in me.Cases) {
                Contract.Assert( mc.Ctor != null);  // the field is filled in by resolution
                Bpl.Axiom ax = FunctionAxiom(f, mc.Body, formal, mc.Ctor, mc.Arguments);
                sink.TopLevelDeclarations.Add(ax);
              }
            } else {
              Bpl.Axiom ax = FunctionAxiom(f, f.Body, null, null, null);
              sink.TopLevelDeclarations.Add(ax);
            }
            if (f.IsRecursive && !f.IsUnlimited) {
              AddLimitedAxioms(f);
            }
          }
          AddFrameAxiom(f);
          AddWellformednessCheck(f);
          
        } else if (member is Method) {
          Method m = (Method)member;
          // wellformedness check for method specification
          Bpl.Procedure proc = AddMethod(m, true, false);
          sink.TopLevelDeclarations.Add(proc);
          AddMethodImpl(m, proc, true);
          // the method itself
          proc = AddMethod(m, false, false);
          sink.TopLevelDeclarations.Add(proc);
          if (m.Body != null) {
            // ...and its implementation
            AddMethodImpl(m, proc, false);
          }
          
          // refinement condition
          if (member is MethodRefinement) {
            AddMethodRefinement((MethodRefinement)member);
          }
        
        } else if (member is CouplingInvariant) {
          // TODO: define a well-foundedness condition to check          

        } else {
          Contract.Assert(false); throw new cce.UnreachableException();  // unexpected member
        }
      }
    }
    
    Bpl.Axiom/*!*/ FunctionAxiom(Function/*!*/ f, Expression/*!*/ body, Formal specializationFormal,
                             DatatypeCtor ctor, List<BoundVar/*!*/> specializationReplacementFormals){
      Contract.Requires(f != null);
      Contract.Requires(body != null);
      Contract.Requires(cce.NonNullElements(specializationReplacementFormals));
     Contract.Requires( predef != null);
     Contract.Requires( specializationFormal == null && ctor == null);
     Contract.Requires( specializationFormal == null && specializationReplacementFormals == null);
     Contract.Requires( f.EnclosingClass != null);
    
      ExpressionTranslator etran = new ExpressionTranslator(this, predef, f.tok);
      
      // axiom
      //   mh < ModuleContextHeight || (mh == ModuleContextHeight && (fh < FunctionContextHeight || InMethodContext))
      //   ==>
      //   (forall $Heap, formals :: this != null && $IsHeap($Heap) && Pre($Heap,args) ==> f(args) == body);
      //
      // The variables "formals" are the formals of function "f"; except, if a specialization is provided, then
      // "specializationFormal" (which is expected to be among the formals of "f") is excluded and replaced by
      // "specializationReplacementFormals".
      // The list "args" is the list of formals of function "f"; except, if a specialization is provided, then
      // "specializationFormal" is replaced by the expression "ctor(specializationReplacementFormals)".
      // If a specialization is provided, occurrences of "specializationFormal" in "body" are also replaced by
      // that expression.
      //
      // The translation of "body" uses the #limited form whenever the callee is in the same SCC of the call graph.
      //
      // Note, an antecedent $Heap[this,alloc] is intentionally left out:  including it would only weaken
      // the axiom.  Moreover, leaving it out does not introduce any soundness problem, because the Dafny
      // allocation statement changes only an allocation bit and then re-assumes $IsGoodHeap; so if it is
      // sound after that, then it would also have been sound just before the allocation.
      //
      Bpl.VariableSeq formals = new Bpl.VariableSeq();
      Bpl.ExprSeq args = new Bpl.ExprSeq();
      Bpl.BoundVariable bv = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, predef.HeapVarName, predef.HeapType));
      formals.Add(bv);
      args.Add(new Bpl.IdentifierExpr(f.tok, bv));
      Bpl.BoundVariable bvThis;
      Bpl.Expr bvThisIdExpr;
      if (f.IsStatic) {
        bvThis = null;
        bvThisIdExpr = null;
      } else {
        bvThis = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, "this", predef.RefType));
        formals.Add(bvThis);
        bvThisIdExpr = new Bpl.IdentifierExpr(f.tok, bvThis);
        args.Add(bvThisIdExpr);
      }
      DatatypeValue r = null;
      if (specializationReplacementFormals != null) {
        Contract.Assert( ctor != null);  // follows from if guard and the precondition
        List<Expression> rArgs = new List<Expression>();
        foreach (BoundVar p in specializationReplacementFormals) {
          bv = new Bpl.BoundVariable(p.tok, new Bpl.TypedIdent(p.tok, p.UniqueName, TrType(p.Type)));
          formals.Add(bv);
          IdentifierExpr ie = new IdentifierExpr(p.tok, p.UniqueName);
          ie.Var = p;  ie.Type = ie.Var.Type;  // resolve it here
          rArgs.Add(ie);
        }
        r = new DatatypeValue(f.tok, cce.NonNull(ctor.EnclosingDatatype).Name, ctor.Name, rArgs);
        r.Ctor = ctor;  r.Type = new UserDefinedType(f.tok, ctor.EnclosingDatatype.Name, new List<Type>()/*this is not right, but it seems like it won't matter here*/);  // resolve it here
      }
      foreach (Formal p in f.Formals) {
        if (p != specializationFormal) {
          bv = new Bpl.BoundVariable(p.tok, new Bpl.TypedIdent(p.tok, p.UniqueName, TrType(p.Type)));
          formals.Add(bv);
          args.Add(new Bpl.IdentifierExpr(p.tok, bv));
        } else {
          Contract.Assert( r != null);  // it is set above
          args.Add(etran.TrExpr(r));
        }
      }

      // mh < ModuleContextHeight || (mh == ModuleContextHeight && (fh < FunctionContextHeight || InMethodContext))
      ModuleDecl m = f.EnclosingClass.Module;
      Bpl.Expr activate = Bpl.Expr.Or(
        Bpl.Expr.Lt(Bpl.Expr.Literal(m.Height), etran.ModuleContextHeight()),
        Bpl.Expr.And(
          Bpl.Expr.Eq(Bpl.Expr.Literal(m.Height), etran.ModuleContextHeight()),
          Bpl.Expr.Or(
            Bpl.Expr.Lt(Bpl.Expr.Literal(m.CallGraph.GetSCCRepresentativeId(f)), etran.FunctionContextHeight()),
            etran.InMethodContext())));
      
      Bpl.Expr ante = FunctionCall(f.tok, BuiltinFunction.IsGoodHeap, null, etran.HeapExpr);
      if (!f.IsStatic) {
        Contract.Assert( bvThisIdExpr != null);  // set to non-null value above when !f.IsStatic
        ante = Bpl.Expr.And(ante, Bpl.Expr.Neq(bvThisIdExpr, predef.Null));
      }
      foreach (Expression req in f.Req) {
        ante = Bpl.Expr.And(ante, etran.TrExpr(req));
      }
      Bpl.FunctionCall funcID = new Bpl.FunctionCall(new Bpl.IdentifierExpr(f.tok, f.FullName, TrType(f.ResultType)));
      Bpl.Expr funcAppl = new Bpl.NAryExpr(f.tok, funcID, args);
      Bpl.Trigger tr = new Bpl.Trigger(f.tok, true, new Bpl.ExprSeq(funcAppl));
      Bpl.TypeVariableSeq typeParams = TrTypeParamDecls(f.TypeArgs);
      if (specializationFormal != null) {
        Contract.Assert( r != null);
        Dictionary<IVariable,Expression> substMap = new Dictionary<IVariable,Expression>();
        substMap.Add(specializationFormal, r);
        body = Substitute(body, null, substMap);
      }
      Bpl.Expr ax = new Bpl.ForallExpr(f.tok, typeParams, formals, null, tr, Bpl.Expr.Imp(ante, Bpl.Expr.Eq(funcAppl, etran.LimitedFunctions(f).TrExpr(body))));
      return new Bpl.Axiom(f.tok, Bpl.Expr.Imp(activate, ax));
    }
    
    void AddLimitedAxioms(Function f){
      Contract.Requires(f != null);
     Contract.Requires( f.IsRecursive && !f.IsUnlimited);
     Contract.Requires( sink != null && predef != null);
      // axiom (forall formals :: { f(args) } f(args) == f#limited(args))

      Bpl.VariableSeq formals = new Bpl.VariableSeq();
      Bpl.ExprSeq args = new Bpl.ExprSeq();
      Bpl.BoundVariable bv = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, predef.HeapVarName, predef.HeapType));
      formals.Add(bv);
      args.Add(new Bpl.IdentifierExpr(f.tok, bv));
      Bpl.BoundVariable bvThis;
      Bpl.Expr bvThisIdExpr;
      if (f.IsStatic) {
        bvThis = null;
        bvThisIdExpr = null;
      } else {
        bvThis = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, "this", predef.RefType));
        formals.Add(bvThis);
        bvThisIdExpr = new Bpl.IdentifierExpr(f.tok, bvThis);
        args.Add(bvThisIdExpr);
      }
      foreach (Formal p in f.Formals) {
        bv = new Bpl.BoundVariable(p.tok, new Bpl.TypedIdent(p.tok, p.UniqueName, TrType(p.Type)));
        formals.Add(bv);
        args.Add(new Bpl.IdentifierExpr(p.tok, bv));
      }
      
      Bpl.FunctionCall origFuncID = new Bpl.FunctionCall(new Bpl.IdentifierExpr(f.tok, f.FullName, TrType(f.ResultType)));
      Bpl.Expr origFuncAppl = new Bpl.NAryExpr(f.tok, origFuncID, args);
      Bpl.FunctionCall limitedFuncID = new Bpl.FunctionCall(new Bpl.IdentifierExpr(f.tok, f.FullName + "#limited", TrType(f.ResultType)));
      Bpl.Expr limitedFuncAppl = new Bpl.NAryExpr(f.tok, limitedFuncID, args);
      
      Bpl.TypeVariableSeq typeParams = TrTypeParamDecls(f.TypeArgs);

      Bpl.Trigger tr = new Bpl.Trigger(f.tok, true, new Bpl.ExprSeq(origFuncAppl));
      Bpl.Expr ax = new Bpl.ForallExpr(f.tok, typeParams, formals, null, tr, Bpl.Expr.Eq(origFuncAppl, limitedFuncAppl));
      sink.TopLevelDeclarations.Add(new Bpl.Axiom(f.tok, ax));
    }
    
    void AddAllocationAxiom(Field f)
    {
      Contract.Requires(f != null);
      Contract.Requires( sink != null && predef != null);
    
      if (f.Type is BoolType || f.Type is IntType || f.Type.IsTypeParameter) {
        return;
      }
      
      Bpl.BoundVariable hVar = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, "$h", predef.HeapType));
      Bpl.Expr h = new Bpl.IdentifierExpr(f.tok, hVar);
      ExpressionTranslator etran = new ExpressionTranslator(this, predef, h);
      Bpl.BoundVariable oVar = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, "$o", predef.RefType));
      Bpl.Expr o = new Bpl.IdentifierExpr(f.tok, oVar);
      
      // h[o,f]
      Bpl.Expr oDotF = Bpl.Expr.SelectTok(f.tok, h, o, new Bpl.IdentifierExpr(f.tok, GetField(f)));
      // $IsGoodHeap(h) && o != null && h[o,alloc]
      Bpl.Expr ante = Bpl.Expr.And(Bpl.Expr.And(
        FunctionCall(f.tok, BuiltinFunction.IsGoodHeap, null, h),
        Bpl.Expr.Neq(o, predef.Null)),
        etran.IsAlloced(f.tok, o));

      if (f.Type is SetType) {
        SetType st = (SetType)f.Type;
        if (st.Arg.IsRefType) {
          //   axiom (forall h: [ref, Field x]x, o: ref, t: BoxType ::
          //        { h[o,f][t] }
          //        $IsGoodHeap(h) && o != null && h[o,alloc] && h[o,f][t] ==> UnBox(t) == null || h[UnBox(t), alloc]);
          Bpl.BoundVariable tVar = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, "$t", predef.BoxType));
          Bpl.Expr t = new Bpl.IdentifierExpr(f.tok, tVar);
          Bpl.Expr oDotFsubT = Bpl.Expr.SelectTok(f.tok, oDotF, t);
          Bpl.Expr unboxT = FunctionCall(f.tok, BuiltinFunction.Unbox, predef.RefType, t);

          Bpl.Trigger tr = new Bpl.Trigger(f.tok, true, new Bpl.ExprSeq(oDotFsubT));
          
          Bpl.Expr goodRef = etran.GoodRef(f.tok, unboxT, st.Arg);
          Bpl.Expr body = Bpl.Expr.Imp(Bpl.Expr.And(ante, oDotFsubT), Bpl.Expr.Or(Bpl.Expr.Eq(unboxT, predef.Null), goodRef));
          Bpl.Expr ax = new Bpl.ForallExpr(f.tok, new Bpl.VariableSeq(hVar, oVar, tVar), tr, body);
          sink.TopLevelDeclarations.Add(new Bpl.Axiom(f.tok, ax));
        } else {
          // TODO: should also handle sets of sets, etc.  This will probably require some extra predicates, and these predicates may even replace the above treatment of sets.
        }
        
      } else if (f.Type is SeqType) {
        SeqType st = (SeqType)f.Type;
        if (st.Arg.IsRefType) {
          //   axiom (forall h: [ref, Field x]x, o: ref, i: int ::
          //        { Seq#Index(h[o,f], i) }
          //        $IsGoodHeap(h) && o != null && h[o,alloc] && 0 <= i && i < Seq#Length(h[o,f]) ==> Unbox(Seq#Index(h[o,f], i)) == null || h[Unbox(Seq#Index(h[o,f], i)), alloc]);
          Bpl.BoundVariable iVar = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, "$i", Bpl.Type.Int));
          Bpl.Expr i = new Bpl.IdentifierExpr(f.tok, iVar);
          Bpl.Expr oDotFsubI = FunctionCall(f.tok, BuiltinFunction.SeqIndex, predef.BoxType, oDotF, i);
          Bpl.Expr unbox = FunctionCall(f.tok, BuiltinFunction.Unbox, predef.RefType, oDotFsubI);
          
          Bpl.Expr range = InSeqRange(f.tok, i, oDotF, true, null, false);
          
          Bpl.Trigger tr = new Bpl.Trigger(f.tok, true, new Bpl.ExprSeq(oDotFsubI));
          
          Bpl.Expr goodRef = etran.GoodRef(f.tok, unbox, st.Arg);
          Bpl.Expr body = Bpl.Expr.Imp(Bpl.Expr.And(ante, range), Bpl.Expr.Or(Bpl.Expr.Eq(unbox, predef.Null), goodRef));
          Bpl.Expr ax = new Bpl.ForallExpr(f.tok, new Bpl.VariableSeq(hVar, oVar, iVar), tr, body);
          sink.TopLevelDeclarations.Add(new Bpl.Axiom(f.tok, ax));
        } else {
          // TODO: should also handle sequences of sequences, etc.  This will probably require some extra predicates, and these predicates may even replace the above treatment of sequences.
        }
      
      } else if (f.Type.IsDatatype) {
        // TODO
        
      } else {
        // reference type:
        //   axiom (forall h: [ref, Field x]x, o: ref ::
        //        { h[o,f] }
        //        $IsGoodHeap(h) && o != null && h[o,alloc] ==> h[o,f] == null || h[h[o,f], alloc]);
        Bpl.Trigger tr = new Bpl.Trigger(f.tok, true, new Bpl.ExprSeq(oDotF));
        
        Bpl.Expr goodRef = etran.GoodRef(f.tok, oDotF, f.Type);
        Bpl.Expr body = Bpl.Expr.Imp(ante, Bpl.Expr.Or(Bpl.Expr.Eq(oDotF, predef.Null), goodRef));
        Bpl.Expr ax = new Bpl.ForallExpr(f.tok, new Bpl.VariableSeq(hVar, oVar), tr, body);
        sink.TopLevelDeclarations.Add(new Bpl.Axiom(f.tok, ax));
      }
    }

    Bpl.Expr InSeqRange(IToken tok, Bpl.Expr index, Bpl.Expr seq, bool isSequence, Bpl.Expr lowerBound, bool includeUpperBound) {
      Contract.Requires(tok != null);
      Contract.Requires(index != null);
      Contract.Requires(seq != null);
      Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

      if (lowerBound == null) {
        lowerBound = Bpl.Expr.Literal(0);
      }
      Bpl.Expr lower = Bpl.Expr.Le(lowerBound, index);
      Bpl.Expr length = FunctionCall(tok, isSequence ? BuiltinFunction.SeqLength : BuiltinFunction.ArrayLength, null, seq);
      Bpl.Expr upper;
      if (includeUpperBound) {
        upper = Bpl.Expr.Le(index, length);
      } else {
        upper = Bpl.Expr.Lt(index, length);
      }
      return Bpl.Expr.And(lower, upper);
    }
    
    Method currentMethod = null;  // the method whose implementation is currently being translated
    int loopHeapVarCount = 0;
    int otherTmpVarCount = 0;
    Bpl.IdentifierExpr _phvie = null;
    Bpl.IdentifierExpr GetPrevHeapVar_IdExpr(IToken tok, Bpl.VariableSeq locals){  // local variable that's shared between statements that need it
      Contract.Requires(tok != null);
      Contract.Requires(locals != null); Contract.Requires( predef != null);
      Contract.Ensures(Contract.Result<Bpl.IdentifierExpr>() != null);

    
    
      if (_phvie == null) {
        // the "tok" of the first request for this variable is the one we use
        Bpl.LocalVariable prevHeapVar = new Bpl.LocalVariable(tok, new Bpl.TypedIdent(tok, "$prevHeap", predef.HeapType));
        locals.Add(prevHeapVar);
        _phvie = new Bpl.IdentifierExpr(tok, prevHeapVar);
      }
      return _phvie;
    }
    Bpl.IdentifierExpr _nwie = null;
    Bpl.IdentifierExpr GetNewVar_IdExpr(IToken tok, Bpl.VariableSeq locals)  // local variable that's shared between statements that need it
    { Contract.Requires(tok != null);
      Contract.Requires(locals != null);
      Contract.Requires( predef != null);

    Contract.Ensures(Contract.Result<Bpl.IdentifierExpr>() != null);

    
      if (_nwie == null) {
        // the "tok" of the first request for this variable is the one we use
        Bpl.LocalVariable nwVar = new Bpl.LocalVariable(tok, new Bpl.TypedIdent(tok, "$nw", predef.RefType));  // important: no where clause (that's why we're going through the trouble of setting of this variable in the first place)
        locals.Add(nwVar);
        _nwie = new Bpl.IdentifierExpr(tok, nwVar);
      }
      return _nwie;
    }

    void AddMethodImpl(Method m, Bpl.Procedure proc, bool wellformednessProc)
    {
      Contract.Requires(m != null);
      Contract.Requires(proc != null);
      Contract.Requires( sink != null && predef != null);
      Contract.Requires( wellformednessProc || m.Body != null);
      Contract.Requires( currentMethod == null && loopHeapVarCount == 0 && _phvie == null && _nwie == null);
      Contract.Ensures(  currentMethod == null && loopHeapVarCount == 0 && _phvie == null && _nwie == null);
    
      currentMethod = m;

      Bpl.TypeVariableSeq typeParams = TrTypeParamDecls(m.TypeArgs);
      Bpl.VariableSeq inParams = Bpl.Formal.StripWhereClauses(proc.InParams);
      Bpl.VariableSeq outParams = Bpl.Formal.StripWhereClauses(proc.OutParams);

      Bpl.StmtListBuilder builder = new Bpl.StmtListBuilder();
      ExpressionTranslator etran = new ExpressionTranslator(this, predef, m.tok);
      Bpl.VariableSeq localVariables = new Bpl.VariableSeq();
      GenerateImplPrelude(m, inParams, outParams, builder, localVariables);
      Bpl.StmtList stmts;
      if (!wellformednessProc) {
        // translate the body of the method
        Contract.Assert( m.Body != null);  // follows from method precondition and the if guard
        stmts = TrStmt2StmtList(builder, m.Body, localVariables, etran);
      } else {
        // check well-formedness of the preconditions, and then assume each one of them
        foreach (MaybeFreeExpression p in m.Req) {
          CheckWellformed(p.E, null, Position.Positive, localVariables, builder, etran);
          builder.Add(new Bpl.AssumeCmd(p.E.tok, etran.TrExpr(p.E)));
        }
        // Note: the modifies clauses are not checked for well-formedness (is that sound?), because it used to
        // be that the syntax was not rich enough for programmers to specify modifies clauses and always being
        // absolutely well-defined.
        // check well-formedness of the decreases clauses
        foreach (Expression p in m.Decreases) {
          CheckWellformed(p, null, Position.Positive, localVariables, builder, etran);
        }

        // play havoc with the heap according to the modifies clause
        builder.Add(new Bpl.HavocCmd(m.tok, new Bpl.IdentifierExprSeq((Bpl.IdentifierExpr/*TODO: this cast is rather dubious*/)etran.HeapExpr)));
        // assume (forall<alpha> o: ref, f: Field alpha :: { $Heap[o,f] } $_Frame[o] || $Heap[o,f] == old($Heap)[o,f]);
        Bpl.TypeVariable alpha = new Bpl.TypeVariable(m.tok, "alpha");
        Bpl.BoundVariable oVar = new Bpl.BoundVariable(m.tok, new Bpl.TypedIdent(m.tok, "$o", predef.RefType));
        Bpl.IdentifierExpr o = new Bpl.IdentifierExpr(m.tok, oVar);
        Bpl.BoundVariable fVar = new Bpl.BoundVariable(m.tok, new Bpl.TypedIdent(m.tok, "$f", predef.FieldName(m.tok, alpha)));
        Bpl.IdentifierExpr f = new Bpl.IdentifierExpr(m.tok, fVar);
      
        Bpl.Expr heapOF = Bpl.Expr.SelectTok(m.tok, etran.HeapExpr, o, f);
        Bpl.Expr oldHeapOF = Bpl.Expr.SelectTok(m.tok, etran.Old.HeapExpr, o, f);
        Bpl.Expr inEnclosingFrame = Bpl.Expr.Select(etran.Old.TheFrame(m.tok), o, f);
        Bpl.Expr body = Bpl.Expr.Or(inEnclosingFrame, Bpl.Expr.Eq(heapOF, oldHeapOF));
        Bpl.Trigger tr = new Bpl.Trigger(m.tok, true, new Bpl.ExprSeq(heapOF));
        Bpl.Expr qq = new Bpl.ForallExpr(m.tok, new Bpl.TypeVariableSeq(alpha), new Bpl.VariableSeq(oVar, fVar), null, tr, body);
        builder.Add(new Bpl.AssumeCmd(m.tok, qq));

        // also play havoc with the out parameters
        if (outParams.Length != 0) {  // don't create an empty havoc statement
          Bpl.IdentifierExprSeq outH = new Bpl.IdentifierExprSeq();
          foreach (Bpl.Variable b in outParams) {
            Contract.Assert(b != null);
            outH.Add(new Bpl.IdentifierExpr(b.tok, b));
          }
          builder.Add(new Bpl.HavocCmd(m.tok, outH));
        }

        // check wellformedness of postconditions
        foreach (MaybeFreeExpression p in m.Ens) {
          CheckWellformed(p.E, null, Position.Positive, localVariables, builder, etran);
          builder.Add(new Bpl.AssumeCmd(p.E.tok, etran.TrExpr(p.E)));
        }

        stmts = builder.Collect(m.tok);
      }

      Bpl.Implementation impl = new Bpl.Implementation(m.tok, proc.Name,
        typeParams, inParams, outParams,
        localVariables, stmts);
      sink.TopLevelDeclarations.Add(impl);
      
      currentMethod = null;
      loopHeapVarCount = 0;
      otherTmpVarCount = 0;
      _phvie = null;
      _nwie = null;
    }

    void GenerateImplPrelude(Method m, Bpl.VariableSeq inParams, Bpl.VariableSeq outParams,
                             Bpl.StmtListBuilder builder, Bpl.VariableSeq localVariables){
      Contract.Requires(m != null);
      Contract.Requires(inParams != null);
      Contract.Requires(outParams != null);
      Contract.Requires(builder != null);
      Contract.Requires(localVariables != null);
     Contract.Requires( predef != null);
    
      // Add CEV prelude
      CEVPrelude(m, inParams, outParams, builder);

      // set up the information used to verify the method's modifies clause
      DefineFrame(m.tok, m.Mod, builder, localVariables);
    }
    
    void CEVPrelude(Method m, Bpl.VariableSeq inParams, Bpl.VariableSeq outParams, Bpl.StmtListBuilder builder) 
    {
      Contract.Requires(m != null);
      Contract.Requires(inParams != null);
      Contract.Requires(outParams != null);
      Contract.Requires(builder != null);
      Contract.Requires( predef != null);
    
      builder.Add(Bpl.Cmd.SimpleAssign(m.tok, Bpl.Expr.Ident("#cev_pc", Bpl.Type.Int), Bpl.Expr.Add(Bpl.Expr.Ident("#cev_pc", Bpl.Type.Int), Bpl.Expr.Literal(1))));
      foreach (Bpl.Variable p in inParams) {
        Contract.Assert( p != null);
        builder.Add(new Bpl.CallCmd(p.tok, "CevVarIntro",
          new Bpl.ExprSeq(
            CevLocation(p.tok),
            new Bpl.IdentifierExpr(p.tok, "cev_parameter", predef.CevVariableKind),
            CevVariable(p.tok, p.Name),
            new Bpl.IdentifierExpr(p.tok, p.Name, p.TypedIdent.Type)),  // it's important not to pass in just p, because that resolves to the proc's param, not the impl's param
          new Bpl.IdentifierExprSeq()));
      }
      foreach (Bpl.Variable p in outParams) {
        Contract.Assert( p != null);
        builder.Add(new Bpl.CallCmd(p.tok, "CevVarIntro",
          new Bpl.ExprSeq(
            CevLocation(p.tok),
            new Bpl.IdentifierExpr(p.tok, "cev_local", predef.CevVariableKind),  // treat out-parameters as locals
            CevVariable(p.tok, p.Name),
            new Bpl.IdentifierExpr(p.tok, p.Name, p.TypedIdent.Type)),  // it's important not to pass in just p, because that resolves to the proc's param, not the impl's param
          new Bpl.IdentifierExprSeq()));
      }      
    }
    
    void DefineFrame(IToken/*!*/ tok, List<FrameExpression/*!*/>/*!*/ frameClause, Bpl.StmtListBuilder/*!*/ builder, Bpl.VariableSeq/*!*/ localVariables){
      Contract.Requires(tok != null);
      Contract.Requires(cce.NonNullElements(frameClause));
      Contract.Requires(builder != null);
      Contract.Requires(cce.NonNullElements(localVariables));
    
    Contract.Requires( predef != null);
    
      ExpressionTranslator etran = new ExpressionTranslator(this, predef, tok);
      // Declare a local variable $_Frame: <alpha>[ref, Field alpha]bool
      Bpl.IdentifierExpr theFrame = etran.TheFrame(tok);  // this is a throw-away expression, used only to extract the name and type of the $_Frame variable
      Contract.Assert( theFrame.Type != null);  // follows from the postcondition of TheFrame
      Bpl.LocalVariable frame = new Bpl.LocalVariable(tok, new Bpl.TypedIdent(tok, theFrame.Name, theFrame.Type));
      localVariables.Add(frame);
      // $_Frame := (lambda<alpha> $o: ref, $f: Field alpha :: $o != null && $Heap[$o,alloc] ==> ($o,$f) in Modifies/Reads-Clause);
      Bpl.TypeVariable alpha = new Bpl.TypeVariable(tok, "alpha");
      Bpl.BoundVariable oVar = new Bpl.BoundVariable(tok, new Bpl.TypedIdent(tok, "$o", predef.RefType));
      Bpl.IdentifierExpr o = new Bpl.IdentifierExpr(tok, oVar);
      Bpl.BoundVariable fVar = new Bpl.BoundVariable(tok, new Bpl.TypedIdent(tok, "$f", predef.FieldName(tok, alpha)));
      Bpl.IdentifierExpr f = new Bpl.IdentifierExpr(tok, fVar);
      Bpl.Expr ante = Bpl.Expr.And(Bpl.Expr.Neq(o, predef.Null), etran.IsAlloced(tok, o));
      Bpl.Expr consequent = InRWClause(tok, o, f, frameClause, etran, null, null);
      Bpl.Expr lambda = new Bpl.LambdaExpr(tok, new Bpl.TypeVariableSeq(alpha), new Bpl.VariableSeq(oVar, fVar), null,
                                           Bpl.Expr.Imp(ante, consequent));
    
      builder.Add(Bpl.Cmd.SimpleAssign(tok, new Bpl.IdentifierExpr(tok, frame), lambda));
    }

    void CheckFrameSubset(IToken tok, List<FrameExpression/*!*/>/*!*/ calleeFrame,
                          Expression receiverReplacement, Dictionary<IVariable,Expression/*!*/> substMap,
                          ExpressionTranslator/*!*/ etran, Bpl.StmtListBuilder/*!*/ builder, string errorMessage)
     
    {
      Contract.Requires(tok != null);
      Contract.Requires(cce.NonNullElements(calleeFrame));
      Contract.Requires(receiverReplacement != null);
      Contract.Requires(cce.NonNullElements(substMap));
      Contract.Requires(etran != null);
      Contract.Requires(builder != null);
      Contract.Requires(errorMessage != null);
      Contract.Requires( predef != null);
      // emit: assert (forall<alpha> o: ref, f: Field alpha :: o != null && $Heap[o,alloc] && (o,f) in calleeFrame ==> $_Frame[o,f]);
      Bpl.TypeVariable alpha = new Bpl.TypeVariable(tok, "alpha");
      Bpl.BoundVariable oVar = new Bpl.BoundVariable(tok, new Bpl.TypedIdent(tok, "$o", predef.RefType));
      Bpl.IdentifierExpr o = new Bpl.IdentifierExpr(tok, oVar);
      Bpl.BoundVariable fVar = new Bpl.BoundVariable(tok, new Bpl.TypedIdent(tok, "$f", predef.FieldName(tok, alpha)));
      Bpl.IdentifierExpr f = new Bpl.IdentifierExpr(tok, fVar);
      Bpl.Expr ante = Bpl.Expr.And(Bpl.Expr.Neq(o, predef.Null), etran.IsAlloced(tok, o));
      Bpl.Expr oInCallee = InRWClause(tok, o, f, calleeFrame, etran, receiverReplacement, substMap);
      Bpl.Expr inEnclosingFrame = Bpl.Expr.Select(etran.TheFrame(tok), o, f);
      Bpl.Expr q = new Bpl.ForallExpr(tok, new Bpl.TypeVariableSeq(alpha), new Bpl.VariableSeq(oVar, fVar),
                                      Bpl.Expr.Imp(Bpl.Expr.And(ante, oInCallee), inEnclosingFrame));
      builder.Add(Assert(tok, q, errorMessage));
    }
    
    /// <summary>
    /// Generates:
    ///   axiom (forall h0: HeapType, h1: HeapType, formals... ::
    ///        { HeapSucc(h0,h1), F(h1,formals) }
    ///        heaps are well-formed and formals are allocated AND
    ///        HeapSucc(h0,h1)
    ///        AND
    ///        (forall(alpha) o: ref, f: Field alpha ::
    ///            o != null AND h0[o,alloc] AND h1[o,alloc] AND
    ///            o in reads clause of formals in h0
    ///            IMPLIES h0[o,f] == h1[o,f])
    ///        IMPLIES
    ///        F(h0,formals) == F(h1,formals)
    ///      );
    ///
    /// If the function is a recursive, non-unlimited function, then the same axiom is also produced for "F#limited" instead of "F".
    /// </summary>
    void AddFrameAxiom(Function f)
    {
      Contract.Requires(f != null);
      Contract.Requires( sink != null && predef != null);
    
      Bpl.BoundVariable h0Var = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, "$h0", predef.HeapType));
      Bpl.BoundVariable h1Var = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, "$h1", predef.HeapType));
      Bpl.Expr h0 = new Bpl.IdentifierExpr(f.tok, h0Var);
      Bpl.Expr h1 = new Bpl.IdentifierExpr(f.tok, h1Var);
      ExpressionTranslator etran0 = new ExpressionTranslator(this, predef, h0);
      ExpressionTranslator etran1 = new ExpressionTranslator(this, predef, h1);
      
      Bpl.Expr wellFormed = Bpl.Expr.And(
        FunctionCall(f.tok, BuiltinFunction.IsGoodHeap, null, etran0.HeapExpr),
        FunctionCall(f.tok, BuiltinFunction.IsGoodHeap, null, etran1.HeapExpr));
      
      Bpl.TypeVariable alpha = new Bpl.TypeVariable(f.tok, "alpha");
      Bpl.BoundVariable oVar = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, "$o", predef.RefType));
      Bpl.Expr o = new Bpl.IdentifierExpr(f.tok, oVar);
      Bpl.BoundVariable fieldVar = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, "$f", predef.FieldName(f.tok, alpha)));
      Bpl.Expr field = new Bpl.IdentifierExpr(f.tok, fieldVar);
      Bpl.Expr oNotNull = Bpl.Expr.Neq(o, predef.Null);
      Bpl.Expr oNotNullAlloced = Bpl.Expr.And(oNotNull, Bpl.Expr.And(etran0.IsAlloced(f.tok, o), etran1.IsAlloced(f.tok, o)));
      Bpl.Expr unchanged = Bpl.Expr.Eq(Bpl.Expr.SelectTok(f.tok, h0, o, field), Bpl.Expr.SelectTok(f.tok, h1, o, field));
      
      Bpl.Expr heapSucc = FunctionCall(f.tok, BuiltinFunction.HeapSucc, null, h0, h1);
      Bpl.Expr r0 = InRWClause(f.tok, o, field, f.Reads, etran0, null, null);
      Bpl.Expr q0 = new Bpl.ForallExpr(f.tok, new Bpl.TypeVariableSeq(alpha), new Bpl.VariableSeq(oVar, fieldVar),
        Bpl.Expr.Imp(Bpl.Expr.And(oNotNullAlloced, r0), unchanged));
      
      // bvars:  h0, h1, formals
      // f0args:  h0, formals
      // f1args:  h1, formals
      Bpl.VariableSeq bvars = new Bpl.VariableSeq();
      Bpl.ExprSeq f0args = new Bpl.ExprSeq();
      Bpl.ExprSeq f1args = new Bpl.ExprSeq();
      bvars.Add(h0Var);  bvars.Add(h1Var);
      f0args.Add(h0);
      f1args.Add(h1);
      if (!f.IsStatic) {
        Bpl.BoundVariable thVar = new Bpl.BoundVariable(f.tok, new Bpl.TypedIdent(f.tok, "this", predef.RefType));
        Bpl.Expr th = new Bpl.IdentifierExpr(f.tok, thVar);
        bvars.Add(thVar);
        f0args.Add(th);
        f1args.Add(th);

        Type thisType = Resolver.GetThisType(f.tok, cce.NonNull(f.EnclosingClass));
        Bpl.Expr wh = Bpl.Expr.And(Bpl.Expr.Neq(th, predef.Null),
          Bpl.Expr.And(etran0.GoodRef(f.tok, th, thisType), etran1.GoodRef(f.tok, th, thisType)));
        wellFormed = Bpl.Expr.And(wellFormed, wh);
      }

      foreach (Formal p in f.Formals) {
        Bpl.BoundVariable bv = new Bpl.BoundVariable(p.tok, new Bpl.TypedIdent(p.tok, p.UniqueName, TrType(p.Type)));
        bvars.Add(bv);
        Bpl.Expr formal = new Bpl.IdentifierExpr(p.tok, bv);
        f0args.Add(formal);
        f1args.Add(formal);
        Bpl.Expr wh = GetWhereClause(p.tok, formal, p.Type, etran0);
        if (wh != null) { wellFormed = Bpl.Expr.And(wellFormed, wh); }
        wh = GetWhereClause(p.tok, formal, p.Type, etran1);
        if (wh != null) { wellFormed = Bpl.Expr.And(wellFormed, wh); }
      }
      
      string axiomComment = "frame axiom for " + f.FullName;
      Bpl.FunctionCall fn = new Bpl.FunctionCall(new Bpl.IdentifierExpr(f.tok, f.FullName, TrType(f.ResultType)));
      while (fn != null) {
        Bpl.Expr F0 = new Bpl.NAryExpr(f.tok, fn, f0args);
        Bpl.Expr F1 = new Bpl.NAryExpr(f.tok, fn, f1args);
        Bpl.Expr eq = Bpl.Expr.Eq(F0, F1);
        Bpl.Trigger tr = new Bpl.Trigger(f.tok, true, new Bpl.ExprSeq(heapSucc, F1));
        
        Bpl.TypeVariableSeq typeParams = TrTypeParamDecls(f.TypeArgs);
        Bpl.Expr ax = new Bpl.ForallExpr(f.tok, typeParams, bvars, null, tr,
          Bpl.Expr.Imp(Bpl.Expr.And(wellFormed, heapSucc),
          Bpl.Expr.Imp(q0, eq)));
        sink.TopLevelDeclarations.Add(new Bpl.Axiom(f.tok, ax, axiomComment));
        if (axiomComment != null && f.IsRecursive && !f.IsUnlimited) {
          fn = new Bpl.FunctionCall(new Bpl.IdentifierExpr(f.tok, f.FullName + "#limited", TrType(f.ResultType)));
          axiomComment = null;  // the comment goes only with the first frame axiom
        } else {
          break;  // no more frame axioms to produce
        }
      }
    }
    
    Bpl.Expr/*!*/ InRWClause(IToken/*!*/ tok, Bpl.Expr/*!*/ o, Bpl.Expr/*!*/ f, List<FrameExpression/*!*/>/*!*/ rw, ExpressionTranslator/*!*/ etran,
                         Expression receiverReplacement, Dictionary<IVariable,Expression/*!*/> substMap){
      Contract.Requires(tok != null);
      Contract.Requires(o != null);
      Contract.Requires(f != null);
      Contract.Requires(etran != null);
      Contract.Requires(cce.NonNullElements(rw));
      Contract.Requires(cce.NonNullElements(substMap));
     Contract.Requires( predef != null);Contract.Requires( receiverReplacement == null && substMap == null);
      Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

      // requires o to denote an expression of type RefType
      // "rw" is is allowed to contain a WildcardExpr
     
    
      Bpl.Expr disjunction = null;
      foreach (FrameExpression rwComponent in rw) {
        Expression e = rwComponent.E;
        if (substMap != null) {
          Contract.Assert( receiverReplacement != null);
          e = Substitute(e, receiverReplacement, substMap);
        }
        Bpl.Expr disjunct;
        if (e is WildcardExpr) {
          disjunct = Bpl.Expr.True;
        } else if (e.Type is SetType) {
          // old(e)[Box(o)]
          disjunct = etran.TrInSet(tok, o, e, ((SetType)e.Type).Arg);
        } else if (e.Type is SeqType) {
          // (exists i: int :: 0 <= i && i < Seq#Length(old(e)) && Seq#Index(old(e),i) == Box(o))
          Bpl.Expr boxO = FunctionCall(tok, BuiltinFunction.Box, null, o);
          Bpl.Variable iVar = new Bpl.BoundVariable(tok, new Bpl.TypedIdent(tok, "$i", Bpl.Type.Int));
          Bpl.Expr i = new Bpl.IdentifierExpr(tok, iVar);
          Bpl.Expr iBounds = InSeqRange(tok, i, etran.TrExpr(e), true, null, false);
          Bpl.Expr XsubI = FunctionCall(tok, BuiltinFunction.SeqIndex, predef.BoxType, etran.TrExpr(e), i);
          // TODO: the equality in the next line should be changed to one that understands extensionality
          disjunct = new Bpl.ExistsExpr(tok, new Bpl.VariableSeq(iVar), Bpl.Expr.And(iBounds, Bpl.Expr.Eq(XsubI, boxO)));
        } else {
          // o == old(e)
          disjunct = Bpl.Expr.Eq(o, etran.TrExpr(e));
        }
        disjunct = Bpl.Expr.And(IsTotal(e, etran), disjunct);
        if (rwComponent.Field != null) {
          disjunct = Bpl.Expr.And(disjunct, Bpl.Expr.Eq(f, new Bpl.IdentifierExpr(rwComponent.E.tok, GetField(rwComponent.Field))));
        }
        if (disjunction == null) {
          disjunction = disjunct;
        } else {
          disjunction = Bpl.Expr.Or(disjunction, disjunct);
        }
      }
      if (disjunction == null) {
        return Bpl.Expr.False;
      } else {
        return disjunction;
      }
    }
    
    void AddWellformednessCheck(Function f){
    Contract.Requires(f != null);
     Contract.Requires( sink != null && predef != null);
     Contract.Requires( f.EnclosingClass != null);
    
      ExpressionTranslator etran = new ExpressionTranslator(this, predef, f.tok);
      // parameters of the procedure
      Bpl.VariableSeq inParams = new Bpl.VariableSeq();
      if (!f.IsStatic) {
        Bpl.Expr wh = Bpl.Expr.And(
          Bpl.Expr.Neq(new Bpl.IdentifierExpr(f.tok, "this", predef.RefType), predef.Null),
          etran.GoodRef(f.tok, new Bpl.IdentifierExpr(f.tok, "this", predef.RefType), Resolver.GetThisType(f.tok, f.EnclosingClass)));
        Bpl.Formal thVar = new Bpl.Formal(f.tok, new Bpl.TypedIdent(f.tok, "this", predef.RefType, wh), true);
        inParams.Add(thVar);
      }
      foreach (Formal p in f.Formals) {
        Bpl.Type varType = TrType(p.Type);
        Bpl.Expr wh = GetWhereClause(p.tok, new Bpl.IdentifierExpr(p.tok, p.UniqueName, varType), p.Type, etran);
        inParams.Add(new Bpl.Formal(p.tok, new Bpl.TypedIdent(p.tok, p.UniqueName, varType, wh), true));
      }
      Bpl.TypeVariableSeq typeParams = TrTypeParamDecls(f.TypeArgs);
      // the procedure itself
      Bpl.RequiresSeq req = new Bpl.RequiresSeq();
      // free requires mh < ModuleContextHeight || (mh == ModuleContextHeight && fh < FunctionContextHeight);
      ModuleDecl m = f.EnclosingClass.Module;
      Bpl.Expr context = Bpl.Expr.And(
        Bpl.Expr.Eq(Bpl.Expr.Literal(m.Height), etran.ModuleContextHeight()),
        Bpl.Expr.Eq(Bpl.Expr.Literal(m.CallGraph.GetSCCRepresentativeId(f)), etran.FunctionContextHeight()));
      req.Add(Requires(f.tok, true, context, null, null));
      Bpl.Procedure proc = new Bpl.Procedure(f.tok, "CheckWellformed$$" + f.FullName, typeParams, inParams, new Bpl.VariableSeq(),
        req, new Bpl.IdentifierExprSeq(), new Bpl.EnsuresSeq());
      sink.TopLevelDeclarations.Add(proc);

      Bpl.VariableSeq locals = new Bpl.VariableSeq();
      Bpl.StmtListBuilder builder = new Bpl.StmtListBuilder();
      // todo: include CEV information of parameters and the heap
      
      // check well-formedness of the preconditions, and then assume each one of them
      foreach (Expression p in f.Req) {
        // Note, in this well-formedness check, function is passed in as null.  This will cause there to be
        // no reads checks or decreases checks.  This seems like a good idea, and I hope it is also sound.
        CheckWellformed(p, null, Position.Positive, locals, builder, etran);
        builder.Add(new Bpl.AssumeCmd(p.tok, etran.TrExpr(p)));
      }
      // Note: the reads clauses are not checked for well-formedness (is that sound?), because it used to
      // be that the syntax was not rich enough for programmers to specify reads clauses and always being
      // absolutely well-defined.
      // check well-formedness of the decreases clauses
      foreach (Expression p in f.Decreases) {
        // Note, in this well-formedness check, function is passed in as null.  This will cause there to be
        // no reads checks or decreases checks.  This seems like a good idea, and I hope it is also sound.
        CheckWellformed(p, null, Position.Positive, locals, builder, etran);
      }
      // check well-formedness of the body
      if (f.Body != null) {
        DefineFrame(f.tok, f.Reads, builder, locals);
        CheckWellformed(f.Body, f, Position.Positive, locals, builder, etran);
      }

      Bpl.Implementation impl = new Bpl.Implementation(f.tok, proc.Name,
        typeParams,
        Bpl.Formal.StripWhereClauses(proc.InParams),
        Bpl.Formal.StripWhereClauses(proc.OutParams),
        locals, builder.Collect(f.tok));
      sink.TopLevelDeclarations.Add(impl);
    }
    
    Bpl.Expr IsTotal(Expression expr, ExpressionTranslator etran){
      Contract.Requires(expr != null);Contract.Requires(etran != null);
      Contract.Requires( predef != null);
      Contract.Ensures(Contract.Result<Bpl.Expr>() != null);
    
      if (expr is LiteralExpr || expr is ThisExpr || expr is IdentifierExpr || expr is WildcardExpr) {
        return Bpl.Expr.True;
      } else if (expr is DisplayExpression) {
        DisplayExpression e = (DisplayExpression)expr;
        return IsTotal(e.Elements, etran);
      } else if (expr is FieldSelectExpr) {
        FieldSelectExpr e = (FieldSelectExpr)expr;
        if (e.Obj is ThisExpr) {
          return Bpl.Expr.True;
        } else {
          return Bpl.Expr.And(IsTotal(e.Obj, etran), Bpl.Expr.Neq(etran.TrExpr(e.Obj), predef.Null));
        }
      } else if (expr is SeqSelectExpr) {
        SeqSelectExpr e = (SeqSelectExpr)expr;
        bool isSequence = e.Seq.Type is SeqType;
        Bpl.Expr total = IsTotal(e.Seq, etran);
        Bpl.Expr seq = etran.TrExpr(e.Seq);
        Bpl.Expr e0 = null;
        if (e.E0 != null) {
          e0 = etran.TrExpr(e.E0);
          total = BplAnd(total, IsTotal(e.E0, etran));
          total = BplAnd(total, InSeqRange(expr.tok, e0, seq, isSequence, null, !e.SelectOne));
        }
        if (e.E1 != null) {
          total = BplAnd(total, IsTotal(e.E1, etran));
          total = BplAnd(total, InSeqRange(expr.tok, etran.TrExpr(e.E1), seq, isSequence, e0, true));
        }
        return total;
      } else if (expr is SeqUpdateExpr) {
        SeqUpdateExpr e = (SeqUpdateExpr)expr;
        Bpl.Expr total = IsTotal(e.Seq, etran);
        Bpl.Expr seq = etran.TrExpr(e.Seq);
        Bpl.Expr index = etran.TrExpr(e.Index);
        total = BplAnd(total, IsTotal(e.Index, etran));
        total = BplAnd(total, InSeqRange(expr.tok, index, seq, true, null, false));
        total = BplAnd(total, IsTotal(e.Value, etran));
        return total;
      } else if (expr is FunctionCallExpr) {
        FunctionCallExpr e = (FunctionCallExpr)expr;
        Contract.Assert( e.Function != null);  // follows from the fact that expr has been successfully resolved
        // check well-formedness of receiver
        Bpl.Expr r = IsTotal(e.Receiver, etran);
        if (!e.Function.IsStatic && !(e.Receiver is ThisExpr)) {
          r = BplAnd(r, Bpl.Expr.Neq(etran.TrExpr(e.Receiver), predef.Null));
        }
        // check well-formedness of the other parameters
        r = BplAnd(r, IsTotal(e.Args, etran));
        // create a substitution map from each formal parameter to the corresponding actual parameter
        Dictionary<IVariable,Expression> substMap = new Dictionary<IVariable,Expression>();
        for (int i = 0; i < e.Function.Formals.Count; i++) {
          substMap.Add(e.Function.Formals[i], e.Args[i]);
        }
        // check that the preconditions for the call hold
        foreach (Expression p in e.Function.Req) {
          Expression precond = Substitute(p, e.Receiver, substMap);
          r = BplAnd(r, etran.TrExpr(precond));
        }
        return r;
      } else if (expr is DatatypeValue) {
        DatatypeValue dtv = (DatatypeValue)expr;
        return IsTotal(dtv.Arguments, etran);
      } else if (expr is OldExpr) {
        OldExpr e = (OldExpr)expr;
        return new Bpl.OldExpr(expr.tok, IsTotal(e.E, etran));
      } else if (expr is FreshExpr) {
        FreshExpr e = (FreshExpr)expr;
        return IsTotal(e.E, etran);
      } else if (expr is UnaryExpr) {
        UnaryExpr e = (UnaryExpr)expr;
        Bpl.Expr t = IsTotal(e.E, etran);
        if (e.Op == UnaryExpr.Opcode.SeqLength && !(e.E.Type is SeqType)) {
          return Bpl.Expr.And(t, Bpl.Expr.Neq(etran.TrExpr(e.E), predef.Null));
        }
        return t;
      } else if (expr is BinaryExpr) {
        BinaryExpr e = (BinaryExpr)expr;
        Bpl.Expr t0 = IsTotal(e.E0, etran);
        Bpl.Expr t1 = IsTotal(e.E1, etran);
        Bpl.Expr z = null;
        switch (e.ResolvedOp) {
          case BinaryExpr.ResolvedOpcode.And:
          case BinaryExpr.ResolvedOpcode.Imp:
            t1 = Bpl.Expr.Imp(etran.TrExpr(e.E0), t1);
            break;
          case BinaryExpr.ResolvedOpcode.Or:
            t1 = Bpl.Expr.Imp(Bpl.Expr.Not(etran.TrExpr(e.E0)), t1);
            break;
          case BinaryExpr.ResolvedOpcode.Div:
          case BinaryExpr.ResolvedOpcode.Mod:
            z = Bpl.Expr.Neq(etran.TrExpr(e.E1), Bpl.Expr.Literal(0));
            break;
          default:
            break;
        }
        Bpl.Expr r = BplAnd(t0, t1);
        return z == null ? r : BplAnd(r, z);
      } else if (expr is QuantifierExpr) {
        QuantifierExpr e = (QuantifierExpr)expr;
        Bpl.Expr total = IsTotal(e.Body, etran);
        if (total != Bpl.Expr.True) {
          Bpl.VariableSeq bvars = new Bpl.VariableSeq();
          foreach (BoundVar bv in e.BoundVars) {
            bvars.Add(new Bpl.BoundVariable(bv.tok, new Bpl.TypedIdent(bv.tok, bv.UniqueName, TrType(bv.Type))));
          }
          total = new Bpl.ForallExpr(expr.tok, bvars, total);
        }
        return total;
      } else if (expr is ITEExpr) {
        ITEExpr e = (ITEExpr)expr;
        Bpl.Expr total = IsTotal(e.Test, etran);
        Bpl.Expr test = etran.TrExpr(e.Test);
        total = BplAnd(total, Bpl.Expr.Imp(test, IsTotal(e.Thn, etran)));
        total = BplAnd(total, Bpl.Expr.Imp(Bpl.Expr.Not(test), IsTotal(e.Els, etran)));
        return total;
      } else {
        Contract.Assert(false); throw new cce.UnreachableException();  // unexpected expression
      }
    }
    
    Bpl.Expr/*!*/ IsTotal(List<Expression/*!*/>/*!*/ exprs, ExpressionTranslator/*!*/ etran) {
      Contract.Requires(etran != null);
      Contract.Requires(exprs != null);
      Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

      Bpl.Expr total = Bpl.Expr.True;
      foreach (Expression e in exprs) {
        Contract.Assert(e != null);
        total = BplAnd(total, IsTotal(e, etran));
      }
      return total;
    }
    
    Bpl.Expr BplAnd(Bpl.Expr a, Bpl.Expr b) {
      Contract.Requires(a != null);
      Contract.Requires(b != null);
      Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

      if (a == Bpl.Expr.True) {
        return b;
      } else if (b == Bpl.Expr.True) {
        return a;
      } else {
        return Bpl.Expr.And(a, b);
      }
    }
    
    enum Position { Positive, Negative, Neither }
    Position Negate(Position pos) {
      switch (pos) {
        case Position.Positive:  return Position.Negative;
        case Position.Negative:  return Position.Positive;
        case Position.Neither:  return Position.Neither;
        default:  Contract.Assert(false); throw new cce.UnreachableException();  // unexpected Position
      }
    }

    void CheckNonNull(IToken tok, Expression e, Bpl.StmtListBuilder builder, ExpressionTranslator etran){
      Contract.Requires(tok != null);
      Contract.Requires(e != null);
      Contract.Requires(builder != null);
      Contract.Requires(etran != null);
     Contract.Requires( predef != null);
    
      if (e is ThisExpr) {
        // already known to be non-null
      } else {
        builder.Add(Assert(tok, Bpl.Expr.Neq(etran.TrExpr(e), predef.Null), "target object may be null"));
      }
    }

    void CheckWellformed(Expression expr, Function func, Position pos, Bpl.VariableSeq locals, Bpl.StmtListBuilder builder, ExpressionTranslator etran){
      Contract.Requires(expr != null);
      Contract.Requires(locals != null);
      Contract.Requires(builder != null);
      Contract.Requires(etran != null);
     Contract.Requires( predef != null);
    
      if (expr is LiteralExpr || expr is ThisExpr || expr is IdentifierExpr || expr is WildcardExpr) {
        // always allowed
      } else if (expr is DisplayExpression) {
        DisplayExpression e = (DisplayExpression)expr;
        foreach (Expression el in e.Elements) {
          CheckWellformed(el, func, Position.Neither, locals, builder, etran);
        }
      } else if (expr is FieldSelectExpr) {
        FieldSelectExpr e = (FieldSelectExpr)expr;
        CheckWellformed(e.Obj, func, Position.Neither, locals, builder, etran);
        CheckNonNull(expr.tok, e.Obj, builder, etran);
        if (func != null) {
          builder.Add(Assert(expr.tok, Bpl.Expr.SelectTok(expr.tok, etran.TheFrame(expr.tok), etran.TrExpr(e.Obj), GetField(e)), "insufficient reads clause to read field"));
        }
      } else if (expr is SeqSelectExpr) {
        SeqSelectExpr e = (SeqSelectExpr)expr;
        bool isSequence = e.Seq.Type is SeqType;
        CheckWellformed(e.Seq, func, Position.Neither, locals, builder, etran);
        Bpl.Expr seq = etran.TrExpr(e.Seq);
        Bpl.Expr e0 = null;
        if (e.E0 != null) {
          e0 = etran.TrExpr(e.E0);
          CheckWellformed(e.E0, func, Position.Neither, locals, builder, etran);
          builder.Add(Assert(expr.tok, InSeqRange(expr.tok, e0, seq, isSequence, null, !e.SelectOne), "index out of range"));
        }
        if (e.E1 != null) {
          CheckWellformed(e.E1, func, Position.Neither, locals, builder, etran);
          builder.Add(Assert(expr.tok, InSeqRange(expr.tok, etran.TrExpr(e.E1), seq, isSequence, e0, true), "end-of-range beyond length of " + (isSequence ? "sequence" : "array")));
        }
        if (func != null && cce.NonNull(e.Seq.Type).IsArrayType) {
          Contract.Assert( e.E0 != null);
          Bpl.Expr fieldName = FunctionCall(expr.tok, BuiltinFunction.IndexField, null, etran.TrExpr(e.E0));
          builder.Add(Assert(expr.tok, Bpl.Expr.SelectTok(expr.tok, etran.TheFrame(expr.tok), seq, fieldName), "insufficient reads clause to read array element"));
        }
      } else if (expr is SeqUpdateExpr) {
        SeqUpdateExpr e = (SeqUpdateExpr)expr;
        CheckWellformed(e.Seq, func, Position.Neither, locals, builder, etran);
        Bpl.Expr seq = etran.TrExpr(e.Seq);
        Bpl.Expr index = etran.TrExpr(e.Index);
        CheckWellformed(e.Index, func, Position.Neither, locals, builder, etran);
        builder.Add(Assert(expr.tok, InSeqRange(expr.tok, index, seq, true, null, false), "index out of range"));
        CheckWellformed(e.Value, func, Position.Neither, locals, builder, etran);
      } else if (expr is FunctionCallExpr) {
        FunctionCallExpr e = (FunctionCallExpr)expr;
        Contract.Assert( e.Function != null);  // follows from the fact that expr has been successfully resolved
        // check well-formedness of receiver
        CheckWellformed(e.Receiver, func, Position.Neither, locals, builder, etran);
        if (!e.Function.IsStatic && !(e.Receiver is ThisExpr)) {
          CheckNonNull(expr.tok, e.Receiver, builder, etran);
        }
        // check well-formedness of the other parameters
        foreach (Expression arg in e.Args) {
          CheckWellformed(arg, func, Position.Neither, locals, builder, etran);
        }
        // create a local variable for each formal parameter, and assign each actual parameter to the corresponding local
        Dictionary<IVariable,Expression> substMap = new Dictionary<IVariable,Expression>();
        for (int i = 0; i < e.Function.Formals.Count; i++) {
          Formal p = e.Function.Formals[i];
          VarDecl local = new VarDecl(p.tok, p.Name, p.Type, p.IsGhost, null);
          local.type = local.OptionalType;  // resolve local here
          IdentifierExpr ie = new IdentifierExpr(local.Tok, local.UniqueName);
          ie.Var = local;  ie.Type = ie.Var.Type;  // resolve ie here
          substMap.Add(p, ie);
          locals.Add(new Bpl.LocalVariable(local.Tok, new Bpl.TypedIdent(local.Tok, local.UniqueName, TrType(local.Type))));
          Bpl.IdentifierExpr lhs = (Bpl.IdentifierExpr)etran.TrExpr(ie);  // TODO: is this cast always justified?
          Expression ee = e.Args[i];
          Bpl.Cmd cmd = Bpl.Cmd.SimpleAssign(p.tok, lhs, etran.CondApplyBox(p.tok, etran.TrExpr(ee), cce.NonNull(ee.Type), p.Type));
          builder.Add(cmd);
        }
        // check that the preconditions for the call hold
        foreach (Expression p in e.Function.Req) {
          Expression precond = Substitute(p, e.Receiver, substMap);
          builder.Add(Assert(expr.tok, etran.TrExpr(precond), "possible violation of function precondition"));
        }
        if (func != null) {
          // check that the callee reads only what the caller is already allowed to read
          CheckFrameSubset(expr.tok, e.Function.Reads, e.Receiver, substMap, etran, builder, "insufficient reads clause to invoke function");
  
          // finally, check that the decreases measure goes down
          ModuleDecl module = cce.NonNull(e.Function.EnclosingClass).Module;
          if (module == cce.NonNull(func.EnclosingClass).Module) {
            if (module.CallGraph.GetSCCRepresentative(e.Function) == module.CallGraph.GetSCCRepresentative(func)) {
              List<Expression> contextDecreases = func.Decreases;
              if (contextDecreases.Count == 0) {
                contextDecreases = new List<Expression>();
                contextDecreases.Add(FrameToObjectSet(func.Reads));  // use its reads clause instead
              }
              List<Expression> calleeDecreases = e.Function.Decreases;
              if (calleeDecreases.Count == 0) {
                calleeDecreases = new List<Expression>();
                calleeDecreases.Add(FrameToObjectSet(e.Function.Reads));  // use its reads clause instead
              }
              CheckCallTermination(expr.tok, contextDecreases, calleeDecreases, e.Receiver, substMap, etran, builder);
            }
          }
        }
      } else if (expr is DatatypeValue) {
        DatatypeValue dtv = (DatatypeValue)expr;
        foreach (Expression arg in dtv.Arguments) {
          CheckWellformed(arg, func, Position.Neither, locals, builder, etran);
        }
      } else if (expr is OldExpr) {
        OldExpr e = (OldExpr)expr;
        CheckWellformed(e.E, func, pos, locals, builder, etran.Old);
      } else if (expr is FreshExpr) {
        FreshExpr e = (FreshExpr)expr;
        CheckWellformed(e.E, func, pos, locals, builder, etran);
      } else if (expr is UnaryExpr) {
        UnaryExpr e = (UnaryExpr)expr;
        CheckWellformed(e.E, func, e.Op == UnaryExpr.Opcode.Not ? Negate(pos) : Position.Neither, locals, builder, etran);
        if (e.Op == UnaryExpr.Opcode.SeqLength && !(e.E.Type is SeqType)) {
          CheckNonNull(expr.tok, e.E, builder, etran);
        }
      } else if (expr is BinaryExpr) {
        BinaryExpr e = (BinaryExpr)expr;
        CheckWellformed(e.E0, func, e.ResolvedOp == BinaryExpr.ResolvedOpcode.Imp ? Negate(pos) : pos, locals, builder, etran);
        switch (e.ResolvedOp) {
          case BinaryExpr.ResolvedOpcode.And:
          case BinaryExpr.ResolvedOpcode.Imp:
            {
              Bpl.StmtListBuilder b = new Bpl.StmtListBuilder();
              CheckWellformed(e.E1, func, pos, locals, b, etran);
              builder.Add(new Bpl.IfCmd(expr.tok, etran.TrExpr(e.E0), b.Collect(expr.tok), null, null));
            }
            break;
          case BinaryExpr.ResolvedOpcode.Or:
            {
              Bpl.StmtListBuilder b = new Bpl.StmtListBuilder();
              CheckWellformed(e.E1, func, pos, locals, b, etran);
              builder.Add(new Bpl.IfCmd(expr.tok, Bpl.Expr.Not(etran.TrExpr(e.E0)), b.Collect(expr.tok), null, null));
            }
            break;
          case BinaryExpr.ResolvedOpcode.Div:
          case BinaryExpr.ResolvedOpcode.Mod:
            CheckWellformed(e.E1, func, pos, locals, builder, etran);
            builder.Add(Assert(expr.tok, Bpl.Expr.Neq(etran.TrExpr(e.E1), Bpl.Expr.Literal(0)), "possible division by zero"));
            break;
          default:
            CheckWellformed(e.E1, func, pos, locals, builder, etran);
            break;
        }

      } else if (expr is QuantifierExpr) {
        QuantifierExpr e = (QuantifierExpr)expr;
        Dictionary<IVariable,Expression> substMap = new Dictionary<IVariable,Expression>();
        foreach (BoundVar bv in e.BoundVars) {
          VarDecl local = new VarDecl(bv.tok, bv.Name, bv.Type, bv.IsGhost, null);
          local.type = local.OptionalType;  // resolve local here
          IdentifierExpr ie = new IdentifierExpr(local.Tok, local.UniqueName);
          ie.Var = local;  ie.Type = ie.Var.Type;  // resolve ie here
          substMap.Add(bv, ie);
          locals.Add(new Bpl.LocalVariable(local.Tok, new Bpl.TypedIdent(local.Tok, local.UniqueName, TrType(local.Type))));
        }
        Expression body = Substitute(e.Body, null, substMap);
        CheckWellformed(body, func, pos, locals, builder, etran);

      } else if (expr is ITEExpr) {
        ITEExpr e = (ITEExpr)expr;
        CheckWellformed(e.Test, func, pos, locals, builder, etran);
        Bpl.StmtListBuilder bThen = new Bpl.StmtListBuilder();
        Bpl.StmtListBuilder bElse = new Bpl.StmtListBuilder();
        CheckWellformed(e.Thn, func, pos, locals, bThen, etran);
        CheckWellformed(e.Els, func, pos, locals, bElse, etran);
        builder.Add(new Bpl.IfCmd(expr.tok, etran.TrExpr(e.Test), bThen.Collect(expr.tok), null, bElse.Collect(expr.tok)));
      
      } else if (expr is MatchExpr) {
        MatchExpr me = (MatchExpr)expr;
        CheckWellformed(me.Source, func, pos, locals, builder, etran);
        Bpl.Expr src = etran.TrExpr(me.Source);
        foreach (MatchCaseExpr mc in me.Cases) {
          Contract.Assert( mc.Ctor != null);  // follows from the fact that mc has been successfully resolved
          Bpl.ExprSeq args = new Bpl.ExprSeq();
          for (int i = 0; i < mc.Arguments.Count; i++) {
            BoundVar p = mc.Arguments[i];
            Bpl.Variable local = new Bpl.LocalVariable(p.tok, new Bpl.TypedIdent(p.tok, p.UniqueName, TrType(p.Type)));
            locals.Add(local);
            IdentifierExpr ie = new IdentifierExpr(p.tok, p.UniqueName);
            ie.Var = p;  ie.Type = ie.Var.Type;  // resolve it here

            Type t = mc.Ctor.Formals[i].Type;
            args.Add(etran.CondApplyBox(expr.tok, new Bpl.IdentifierExpr(p.tok, local), cce.NonNull(p.Type), t));
          }
          Bpl.IdentifierExpr id = new Bpl.IdentifierExpr(mc.tok, mc.Ctor.FullName, predef.DatatypeType);
          Bpl.Expr ct = new Bpl.NAryExpr(mc.tok, new Bpl.FunctionCall(id), args);

          // generate:  if (src == ctor(args)) { mc.Body is well-formed }
          Bpl.StmtListBuilder b = new Bpl.StmtListBuilder();
          CheckWellformed(mc.Body, func, pos, locals, b, etran);
          builder.Add(new Bpl.IfCmd(mc.tok, Bpl.Expr.Eq(src, ct), b.Collect(mc.tok), null, null));
        }
        
      } else {
        Contract.Assert(false); throw new cce.UnreachableException();  // unexpected expression
      }
    }
    
    Expression FrameToObjectSet(List<FrameExpression> fexprs) {
      Contract.Requires(fexprs != null);
      Contract.Ensures(Contract.Result<Expression>() != null);

      List<Expression> sets = new List<Expression>();
      List<Expression> singletons = null;
      foreach (FrameExpression fe in fexprs) {
        Contract.Assert(fe != null);
        if (fe.E is WildcardExpr) {
          // drop wildcards altogether
        } else {
          Expression e = fe.E;  // keep only fe.E, drop any fe.Field designation
          Contract.Assert( e.Type != null);  // should have been resolved already
          if (e.Type.IsRefType) {
            // e represents a singleton set
            if (singletons == null) {
              singletons = new List<Expression>();
            }
            singletons.Add(e);
          } else {
            // e is already a set
            Contract.Assert( e.Type is SetType);
            sets.Add(e);
          }
        }
      }
      if (singletons != null) {
        Expression display = new SetDisplayExpr(singletons[0].tok, singletons);
        display.Type = new SetType(new ObjectType());  // resolve here
        sets.Add(display);
      }
      if (sets.Count == 0) {
        Expression emptyset = new SetDisplayExpr(Token.NoToken, new List<Expression>());
        emptyset.Type = new SetType(new ObjectType());  // resolve here
        return emptyset;
      } else {
        Expression s = sets[0];
        for (int i = 1; i < sets.Count; i++) {
          BinaryExpr union = new BinaryExpr(s.tok, BinaryExpr.Opcode.Add, s, sets[i]);
          union.ResolvedOp = BinaryExpr.ResolvedOpcode.Union;  // resolve here
          union.Type = new SetType(new ObjectType());  // resolve here
          s = union;
        }
        return s;
      }
    }
    
    Bpl.Constant GetClass(TopLevelDecl cl)
     {
      Contract.Requires(cl != null);
 Contract.Requires( predef != null);
      Contract.Ensures(Contract.Result<Bpl.Constant>() != null);

      Bpl.Constant cc;
      if (classes.TryGetValue(cl, out cc)) {
        Contract.Assert( cc != null);
      } else {
        cc = new Bpl.Constant(cl.tok, new Bpl.TypedIdent(cl.tok, "class." + cl.Name, predef.ClassNameType), true);
        classes.Add(cl, cc);
      }
      return cc;
    }
    
    Bpl.Expr GetTypeExpr(IToken tok, Type type)
     {
      Contract.Requires(tok != null);
      Contract.Requires(type != null);
 Contract.Requires( predef != null);
      while (true) {
        TypeProxy tp = type as TypeProxy;
        if (tp == null) {
          break;
        } else if (tp.T == null) {
          // unresolved proxy
          // TODO: what to do here?
          return null;
        } else {
          type = tp.T;
        }
      }

      if (type is BoolType) {
        return new Bpl.IdentifierExpr(tok, "class.bool", predef.ClassNameType);
      } else if (type is IntType) {
        return new Bpl.IdentifierExpr(tok, "class.int", predef.ClassNameType);
      } else if (type is ObjectType) {
        return new Bpl.IdentifierExpr(tok, "class.object", predef.ClassNameType);
      } else if (type is CollectionType) {
        CollectionType ct = (CollectionType)type;
        Bpl.Expr a = GetTypeExpr(tok, ct.Arg);
        if (a == null) {
         return null;
        }
        Bpl.Expr t = new Bpl.IdentifierExpr(tok, ct is SetType ? "class.set" : "class.seq", predef.ClassNameType);
        return FunctionCall(tok, BuiltinFunction.TypeTuple, null, t, a);
      } else {
        UserDefinedType ct = (UserDefinedType)type;
        if (ct.ResolvedClass == null) {
         return null;  // TODO: what to do here?
        }
        Bpl.Expr t = new Bpl.IdentifierExpr(tok, GetClass(ct.ResolvedClass));
        foreach (Type arg in ct.TypeArgs) {
          Bpl.Expr a = GetTypeExpr(tok, arg);
          if (a == null) {
            return null;
          }
          t = FunctionCall(tok, BuiltinFunction.TypeTuple, null, t, a);
        }
        return t;
      }
    }
    
    Bpl.Constant GetField(Field f)
     {
      Contract.Requires(f != null);Contract.Requires( sink != null && predef != null);
      Contract.Ensures(Contract.Result<Bpl.Constant>() != null);

     
      Bpl.Constant fc;
      if (fields.TryGetValue(f, out fc)) {
        Contract.Assert( fc != null);
      } else {
        // const unique f: Field ty;
        Bpl.Type ty = predef.FieldName(f.tok, TrType(f.Type));
        fc = new Bpl.Constant(f.tok, new Bpl.TypedIdent(f.tok, f.FullName, ty), true);
        fields.Add(f, fc);
        // axiom FCat(f) == $NamedField && DeclType(f) == C;
        Bpl.Expr fcat = Bpl.Expr.Eq(FunctionCall(f.tok, BuiltinFunction.FCat, ty, Bpl.Expr.Ident(fc)), new Bpl.IdentifierExpr(f.tok, "$NamedField", predef.FieldCategoryType));
        Bpl.Expr declType = Bpl.Expr.Eq(FunctionCall(f.tok, BuiltinFunction.DeclType, ty, Bpl.Expr.Ident(fc)), new Bpl.IdentifierExpr(f.tok, GetClass(cce.NonNull(f.EnclosingClass))));
        Bpl.Axiom ax = new Bpl.Axiom(f.tok, Bpl.Expr.And(fcat, declType));
        sink.TopLevelDeclarations.Add(ax);
      }
      return fc;
    }
    
    Bpl.Expr GetField(FieldSelectExpr fse)
     {
      Contract.Requires(fse != null);Contract.Requires( fse.Field != null);
      Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

     
      return new Bpl.IdentifierExpr(fse.tok, GetField(fse.Field));
    }

    /// <summary>
    /// This method is expected to be called just once for each function in the program.
    /// </summary>    
    void AddFunction(Function f)
     {
      Contract.Requires(f != null);
     Contract.Requires( predef != null && sink != null);
      Bpl.TypeVariableSeq typeParams = TrTypeParamDecls(f.TypeArgs);
      Bpl.VariableSeq args = new Bpl.VariableSeq();
      args.Add(new Bpl.Formal(f.tok, new Bpl.TypedIdent(f.tok, "$heap", predef.HeapType), true));
      if (!f.IsStatic) {
        args.Add(new Bpl.Formal(f.tok, new Bpl.TypedIdent(f.tok, "this", predef.RefType), true));
      }
      foreach (Formal p in f.Formals) {
        args.Add(new Bpl.Formal(p.tok, new Bpl.TypedIdent(p.tok, p.UniqueName, TrType(p.Type)), true));
      }
      Bpl.Formal res = new Bpl.Formal(f.tok, new Bpl.TypedIdent(f.tok, Bpl.TypedIdent.NoName, TrType(f.ResultType)), false);
      Bpl.Function func = new Bpl.Function(f.tok, f.FullName, typeParams, args, res);
      sink.TopLevelDeclarations.Add(func);
      
      if (f.IsRecursive && !f.IsUnlimited) {
        Bpl.Function limitedF = new Bpl.Function(f.tok, f.FullName + "#limited", args, res);
        sink.TopLevelDeclarations.Add(limitedF);
      }
    }
    
    /// <summary>
    /// This method is expected to be called just twice for each procedure in the program (once with
    /// wellformednessProc set to true, once with wellformednessProc set to false).
    /// In addition, it is used once to generate refinement conditions.
    /// </summary>    
    Bpl.Procedure AddMethod(Method m, bool wellformednessProc, bool skipEnsures)
     {
      Contract.Requires(m != null);
      Contract.Requires( predef != null);
      Contract.Requires( m.EnclosingClass != null);
     Contract.Requires( !skipEnsures || !wellformednessProc);
      Contract.Ensures(Contract.Result<Bpl.Procedure>() != null);


      ExpressionTranslator etran = new ExpressionTranslator(this, predef, m.tok);
      
      Bpl.VariableSeq inParams = new Bpl.VariableSeq();
      Bpl.VariableSeq outParams = new Bpl.VariableSeq();
      if (!m.IsStatic) {
        Bpl.Expr wh = Bpl.Expr.And(
          Bpl.Expr.Neq(new Bpl.IdentifierExpr(m.tok, "this", predef.RefType), predef.Null),
          etran.GoodRef(m.tok, new Bpl.IdentifierExpr(m.tok, "this", predef.RefType), Resolver.GetThisType(m.tok, m.EnclosingClass)));
        Bpl.Formal thVar = new Bpl.Formal(m.tok, new Bpl.TypedIdent(m.tok, "this", predef.RefType, wh), true);
        inParams.Add(thVar);
      }
      foreach (Formal p in m.Ins) {
        Bpl.Type varType = TrType(p.Type);
        Bpl.Expr wh = GetWhereClause(p.tok, new Bpl.IdentifierExpr(p.tok, p.UniqueName, varType), p.Type, etran);
        inParams.Add(new Bpl.Formal(p.tok, new Bpl.TypedIdent(p.tok, p.UniqueName, varType, wh), true));
      }
      foreach (Formal p in m.Outs) {
        Bpl.Type varType = TrType(p.Type);
        Bpl.Expr wh = GetWhereClause(p.tok, new Bpl.IdentifierExpr(p.tok, p.UniqueName, varType), p.Type, etran);
        outParams.Add(new Bpl.Formal(p.tok, new Bpl.TypedIdent(p.tok, p.UniqueName, varType, wh), false));
      }
      
      Bpl.RequiresSeq req = new Bpl.RequiresSeq();
      Bpl.IdentifierExprSeq mod = new Bpl.IdentifierExprSeq();
      Bpl.EnsuresSeq ens = new Bpl.EnsuresSeq();
      // free requires mh == ModuleContextHeight && InMethodContext;
      Bpl.Expr context = Bpl.Expr.And(
        Bpl.Expr.Eq(Bpl.Expr.Literal(m.EnclosingClass.Module.Height), etran.ModuleContextHeight()),
        etran.InMethodContext());
      req.Add(Requires(m.tok, true, context, null, null));
      mod.Add(etran.HeapExpr);
      mod.Add(Bpl.Expr.Ident("#cev_pc", Bpl.Type.Int));
      
      if (!wellformednessProc) {
        string comment = "user-defined preconditions";
        foreach (MaybeFreeExpression p in m.Req) {
          if (p.IsFree) {
            req.Add(Requires(p.E.tok, true, etran.TrExpr(p.E), null, comment));
          } else {
            List<Expression> definitions, pieces;
            if (!SplitExpr(p.E, out definitions, out pieces)) {
              req.Add(Requires(p.E.tok, false, etran.TrExpr(p.E), null, comment));
            } else {
              req.Add(Requires(p.E.tok, true, etran.TrExpr(p.E), null, comment));  // add the entire condition as a free precondition
              Bpl.Expr ante = Bpl.Expr.True;
              foreach (Expression d in definitions) {
                Bpl.Expr trD = etran.TrExpr(d);
                req.Add(Requires(d.tok, true, trD, null, null));
                ante = Bpl.Expr.And(ante, trD);
              }
              foreach (Expression se in pieces) {
                req.Add(Requires(se.tok, false, Bpl.Expr.Imp(ante, etran.TrExpr(se)), null, null));  // TODO: it would be fine to have these use {:subsumption 0}
              }
            }
          }
          comment = null;
        }
        comment = "user-defined postconditions";
        if (!skipEnsures) foreach (MaybeFreeExpression p in m.Ens) {
          if (p.IsFree) {
            ens.Add(Ensures(p.E.tok, true, etran.TrExpr(p.E), null, comment));
          } else {
            List<Expression> definitions, pieces;
            if (!SplitExpr(p.E, out definitions, out pieces)) {
              ens.Add(Ensures(p.E.tok, false, etran.TrExpr(p.E), null, comment));
            } else {
              ens.Add(Ensures(p.E.tok, true, etran.TrExpr(p.E), null, comment));  // add the entire condition as a free postcondition
              Bpl.Expr ante = Bpl.Expr.True;
              foreach (Expression d in definitions) {
                Bpl.Expr trD = etran.TrExpr(d);
                ens.Add(Ensures(d.tok, true, trD, null, null));
                ante = Bpl.Expr.And(ante, trD);
              }
              foreach (Expression se in pieces) {
                ens.Add(Ensures(se.tok, false, Bpl.Expr.Imp(ante, etran.TrExpr(se)), null, null));  // TODO: it would be fine to have these use {:subsumption 0}
              }
            }
          }
          comment = null;
        }
        if (!skipEnsures) foreach (BoilerplateTriple tri in GetTwoStateBoilerplate(m.tok, m, etran.Old, etran)) {
          ens.Add(Ensures(tri.tok, tri.IsFree, tri.Expr, tri.ErrorMessage, tri.Comment));
        }
      }

      // CEV information
      // free requires #cev_pc == 0;
      req.Add(Requires(m.tok, true, Bpl.Expr.Eq(Bpl.Expr.Ident("#cev_pc", Bpl.Type.Int), Bpl.Expr.Literal(0)), null, "CEV information"));
      // free requires #cev_init(0);
      req.Add(Requires(m.tok, true, FunctionCall(m.tok, BuiltinFunction.CevInit, null, Bpl.Expr.Literal(0)), null, null));
      // free requires #cev_var_intro(0, cev_implicit, #loc.$Heap, $Heap, 0);
      req.Add(Requires(m.tok, true, FunctionCall(m.tok, BuiltinFunction.CevVarIntro, null,
        Bpl.Expr.Literal(0),
        new Bpl.IdentifierExpr(m.tok, "cev_implicit", predef.CevVariableKind),
        CevVariable(m.tok, predef.HeapVarName),
        etran.HeapExpr,
        Bpl.Expr.Literal(0)), null, null));
      // free requires #cev_save_position(0) == #tok.Tok;
      req.Add(Requires(m.tok, true, Bpl.Expr.Eq(FunctionCall(m.tok, BuiltinFunction.CevProgramLocation, null, Bpl.Expr.Literal(0)), CevLocation(m.tok)), null, null));
      // free ensures old(#cev_pc) < #cev_pc;
      ens.Add(Ensures(m.tok, true, Bpl.Expr.Lt(new Bpl.OldExpr(m.tok, Bpl.Expr.Ident("#cev_pc", Bpl.Type.Int)), Bpl.Expr.Ident("#cev_pc", Bpl.Type.Int)), null, "CEV information"));

      Bpl.TypeVariableSeq typeParams = TrTypeParamDecls(m.TypeArgs);
      string name = wellformednessProc ? "CheckWellformed$$" + m.FullName : m.FullName;
      Bpl.Procedure proc = new Bpl.Procedure(m.tok, name, typeParams, inParams, outParams, req, mod, ens);
      return proc;
    }

    #region Refinement extension    
    
    void AddMethodRefinement(MethodRefinement m) 
     {
      Contract.Requires(m != null);
     Contract.Requires( sink != null && predef != null);
      // r is abstract, m is concrete 
      Method r = m.Refined;
      Contract.Assert( r != null);
      Contract.Assert( m.EnclosingClass != null);
      string name = "Refinement$$" + m.FullName;
      string that = "that";
      
      Bpl.IdentifierExpr heap = new Bpl.IdentifierExpr(m.tok, predef.HeapVarName, predef.HeapType);
      ExpressionTranslator etran = new ExpressionTranslator(this, predef, heap, that);
      
      // TODO: this straight inlining does not handle recursive calls
      // TODO: we assume frame allows anything to be changed -- we don't include post-conditions in the refinement procedure, or check refinement of frames      
            
      // generate procedure declaration with pre-condition wp(r, true)
      Bpl.Procedure proc = AddMethod(r, false, true);
      proc.Name = name;
      
      // create "that" for m
      Bpl.Expr wh = Bpl.Expr.And(
        Bpl.Expr.Neq(new Bpl.IdentifierExpr(m.tok, that, predef.RefType), predef.Null),
        etran.GoodRef(m.tok, new Bpl.IdentifierExpr(m.tok, that, predef.RefType), Resolver.GetThisType(m.tok, m.EnclosingClass)));
      Bpl.Formal thatVar = new Bpl.Formal(m.tok, new Bpl.TypedIdent(m.tok, that, predef.RefType, wh), true);
      proc.InParams.Add(thatVar);
      
      // add outs of m to the outs of the refinement procedure   
      foreach (Formal p in m.Outs) {
        Bpl.Type varType = TrType(p.Type);
        Bpl.Expr w = GetWhereClause(p.tok, new Bpl.IdentifierExpr(p.tok, p.UniqueName, varType), p.Type, etran);
        proc.OutParams.Add(new Bpl.Formal(p.tok, new Bpl.TypedIdent(p.tok, p.UniqueName, varType, w), false));
      }
      sink.TopLevelDeclarations.Add(proc);
           
      // generate procedure implementation:      
      Bpl.TypeVariableSeq typeParams = TrTypeParamDecls(m.TypeArgs);
      Bpl.VariableSeq inParams = Bpl.Formal.StripWhereClauses(proc.InParams);
      Bpl.VariableSeq outParams = Bpl.Formal.StripWhereClauses(proc.OutParams);
      Bpl.StmtListBuilder builder = new Bpl.StmtListBuilder();
      Bpl.VariableSeq localVariables = new Bpl.VariableSeq();
      
      Contract.Assert( m.Body != null);
      Contract.Assert( r.Body != null);
      
      // declare a frame variable that allows anything to be changed (not checking modifies clauses)
      CEVPrelude(m, inParams, outParams, builder);
      Bpl.IdentifierExpr theFrame = etran.TheFrame(m.tok);
      Contract.Assert( theFrame.Type != null);
      Bpl.LocalVariable frame = new Bpl.LocalVariable(m.tok, new Bpl.TypedIdent(m.tok, theFrame.Name, theFrame.Type));
      localVariables.Add(frame);
      // $_Frame := (lambda<alpha> $o: ref, $f: Field alpha :: true);
      Bpl.TypeVariable alpha = new Bpl.TypeVariable(m.tok, "alpha");
      Bpl.BoundVariable oVar = new Bpl.BoundVariable(m.tok, new Bpl.TypedIdent(m.tok, "$o", predef.RefType));
      Bpl.BoundVariable fVar = new Bpl.BoundVariable(m.tok, new Bpl.TypedIdent(m.tok, "$f", predef.FieldName(m.tok, alpha)));
      Bpl.Expr lambda = new Bpl.LambdaExpr(m.tok, new Bpl.TypeVariableSeq(alpha), new Bpl.VariableSeq(oVar, fVar), null, Bpl.Expr.True);
      builder.Add(Bpl.Cmd.SimpleAssign(m.tok, new Bpl.IdentifierExpr(m.tok, frame), lambda));
      
      // assume I($Heap, $Heap)              
      builder.Add(new Bpl.AssumeCmd(m.tok, TrCouplingInvariant(m, heap, "this", heap, that)));
      
      // assign input formals of m (except "this")
      for (int i = 0; i < m.Ins.Count; i++) {        
        Bpl.LocalVariable arg = new Bpl.LocalVariable(m.tok, new Bpl.TypedIdent(m.tok, m.Ins[i].UniqueName,  TrType(m.Ins[i].Type)));
        localVariables.Add(arg);
        Bpl.Variable var = inParams[i+1];
        Contract.Assert( var != null);
        builder.Add(Bpl.Cmd.SimpleAssign(m.tok, new Bpl.IdentifierExpr(m.tok, arg), new Bpl.IdentifierExpr(m.tok, var)));
      }

      // set-up method-translator state
      currentMethod = m;
      loopHeapVarCount = 0;
      otherTmpVarCount = 0;
      _phvie = null;
      _nwie = null;
            
      //  call inlined m;
      TrStmt(m.Body, builder, localVariables, etran);

      //  $Heap1 := $Heap;
      Bpl.LocalVariable heap2 = new Bpl.LocalVariable(m.tok, new Bpl.TypedIdent(m.tok, heap.Name+"2", predef.HeapType));
      localVariables.Add(heap2);
      builder.Add(Bpl.Cmd.SimpleAssign(m.tok, new Bpl.IdentifierExpr(m.tok, heap2), etran.HeapExpr));
      
      //  $Heap := old($Heap);
      builder.Add(Bpl.Cmd.SimpleAssign(m.tok, heap, new Bpl.OldExpr(m.tok, heap)));
                                
      //  call inlined r;
      currentMethod = r;
      etran = new ExpressionTranslator(this, predef, heap, "this");
      TrStmt(r.Body, builder, localVariables, etran);

      // clean method-translator state
      currentMethod = null;
      loopHeapVarCount = 0;
      otherTmpVarCount = 0;
      _phvie = null;
      _nwie = null;
      
      //  Contract.Assert( output variables of r and m are pairwise equal
      Contract.Assert( outParams.Length % 2 == 0);
      int k = outParams.Length / 2;
      for (int i = 0; i < k; i++) {
        Bpl.Variable rOut = outParams[i];
        Bpl.Variable mOut = outParams[i+k];
        Contract.Assert( rOut != null && mOut != null);
        builder.Add(Assert(m.tok, Bpl.Expr.Eq(new Bpl.IdentifierExpr(m.tok, mOut), new Bpl.IdentifierExpr(m.tok, rOut)), 
          "Refinement method may not produce the same value for output variable " + m.Outs[i].Name));
      }
            
      //  Contract.Assert( I($Heap1, $Heap)      
      builder.Add(Assert(m.tok, TrCouplingInvariant(m, heap, "this", new Bpl.IdentifierExpr(m.tok, heap2), that),
        "Refinement method may not preserve the coupling invariant"));
                                                
      Bpl.StmtList stmts = builder.Collect(m.tok);
      Bpl.Implementation impl = new Bpl.Implementation(m.tok, proc.Name,
        typeParams, inParams, outParams,
        localVariables, stmts);
      sink.TopLevelDeclarations.Add(impl);
    }
    
    private sealed class NominalSubstituter : Duplicator
    {
      private readonly Dictionary<string,Bpl.Expr> subst;
      public NominalSubstituter(Dictionary<string,Bpl.Expr> subst) :base(){
        Contract.Requires(cce.NonNullElements(subst));
        this.subst = subst; 
      }
      
      [ContractInvariantMethod]
void ObjectInvariant() 
{
    Contract.Invariant(cce.NonNullElements(subst));
}

          
      public override Expr VisitIdentifierExpr(Bpl.IdentifierExpr node)
      {
        Contract.Requires(node != null);
        Contract.Ensures(Contract.Result<Expr>() != null);

        if (subst.ContainsKey(node.Name))
          return subst[node.Name];
        else 
          return base.VisitIdentifierExpr(node);
      }
    }

    Bpl.Expr TrCouplingInvariant(MethodRefinement m, Bpl.Expr absHeap, string absThis, Bpl.Expr conHeap, string conThis)
     {
      Contract.Requires(m != null);
      Contract.Requires(absHeap != null);
      Contract.Requires(absThis != null);
      Contract.Requires(conHeap != null);
      Contract.Requires(conThis != null);
      Contract.Requires(predef != null);
      Bpl.Expr cond = Bpl.Expr.True;
      ClassRefinementDecl c = m.EnclosingClass as ClassRefinementDecl;
      Contract.Assert( c != null);
      ExpressionTranslator etran = new ExpressionTranslator(this, predef, conHeap, conThis);
            
      foreach (MemberDecl d in c.Members) 
        if (d is CouplingInvariant) {
          CouplingInvariant inv = (CouplingInvariant)d;

          Contract.Assert( inv.Refined != null);
          Contract.Assert( inv.Formals != null);
          
          // replace formals with field dereferences                    
          Dictionary<string,Bpl.Expr> map = new Dictionary<string,Bpl.Expr>();
          Bpl.Expr absVar = new Bpl.IdentifierExpr(d.tok, absThis, predef.RefType);
          for (int i = 0; i < inv.Refined.Count; i++) {     
            // TODO: boxing/unboxing?
            Bpl.Expr result = Bpl.Expr.SelectTok(inv.Toks[i], absHeap, absVar, new Bpl.IdentifierExpr(inv.Toks[i], GetField(cce.NonNull(inv.Refined[i]))));
            map.Add(inv.Formals[i].UniqueName, result);
          }
                              
          Bpl.Expr e = new NominalSubstituter(map).VisitExpr(etran.TrExpr(inv.Expr));
          cond = Bpl.Expr.And(cond, e);
        }
            
      return cond;
    }
    
    #endregion
    
    class BoilerplateTriple {  // a triple that is now a quintuple
      [ContractInvariantMethod]
void ObjectInvariant() 
{
    Contract.Invariant(tok!=null);
        Contract.Invariant(Expr != null);
        Contract.Invariant(IsFree || ErrorMessage != null);
}

      public readonly IToken tok;
      public readonly bool IsFree;
      public readonly Bpl.Expr Expr;
      public readonly string ErrorMessage;
      public readonly string Comment;


      public BoilerplateTriple(IToken tok, bool isFree, Bpl.Expr expr, string errorMessage, string comment)
       {
        Contract.Requires(tok != null);
        Contract.Requires(expr != null);
       Contract.Requires( isFree || errorMessage != null);
        this.tok = tok;
        IsFree = isFree;
        Expr = expr;
        ErrorMessage = errorMessage;
        Comment = comment;
      }
    }
    
    /// <summary>
    /// There are 3 states of interest when generating two-state boilerplate:
    ///  S0. the beginning of the method, which is where the modifies clause is interpreted
    ///  S1. the pre-state of the two-state interval
    ///  S2. the post-state of the two-state interval
    /// This method assumes that etranPre denotes S1, etran denotes S2, and that etran.Old denotes S0.
    /// </summary>
    List<BoilerplateTriple/*!*/>/*!*/ GetTwoStateBoilerplate(IToken/*!*/ tok, Method/*!*/ method, ExpressionTranslator/*!*/ etranPre, ExpressionTranslator/*!*/ etran)
    {
      Contract.Requires(tok != null);
      Contract.Requires(method != null);
      Contract.Requires(etranPre != null);
      Contract.Requires(etran != null);
      Contract.Ensures(cce.NonNullElements(Contract.Result<List<BoilerplateTriple>>()));

      List<BoilerplateTriple> boilerplate = new List<BoilerplateTriple>();
      
      // the frame condition, which is free since it is checked with every heap update and call
      boilerplate.Add(new BoilerplateTriple(tok, true, FrameCondition(tok, method.Mod, etranPre, etran), null, "frame condition"));

      // HeapSucc(S1, S2)
      Bpl.Expr heapSucc = FunctionCall(tok, BuiltinFunction.HeapSucc, null, etranPre.HeapExpr, etran.HeapExpr);
      boilerplate.Add(new BoilerplateTriple(tok, true, heapSucc, null, "boilerplate"));
      
      return boilerplate;
    }
    
    /// <summary>
    /// There are 3 states of interest when generating a freame condition:
    ///  S0. the beginning of the method, which is where the modifies clause is interpreted
    ///  S1. the pre-state of the two-state interval
    ///  S2. the post-state of the two-state interval
    /// This method assumes that etranPre denotes S1, etran denotes S2, and that etran.Old denotes S0.
    /// </summary>
    Bpl.Expr/*!*/ FrameCondition(IToken/*!*/ tok, List<FrameExpression/*!*/>/*!*/ modifiesClause, ExpressionTranslator/*!*/ etranPre, ExpressionTranslator/*!*/ etran)
     {
      Contract.Requires(tok != null);
      Contract.Requires(etran != null);
      Contract.Requires(etranPre != null);
      Contract.Requires(cce.NonNullElements(modifiesClause));
     Contract.Requires( predef != null);
      Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

      // generate:
      //  (forall<alpha> o: ref, f: Field alpha :: { $Heap[o,f] }
      //      o != null && old($Heap)[o,alloc] ==>
      //        $Heap[o,f] == PreHeap[o,f] ||
      //        (o,f) in modifiesClause)
      Bpl.TypeVariable alpha = new Bpl.TypeVariable(tok, "alpha");
      Bpl.BoundVariable oVar = new Bpl.BoundVariable(tok, new Bpl.TypedIdent(tok, "$o", predef.RefType));
      Bpl.IdentifierExpr o = new Bpl.IdentifierExpr(tok, oVar);
      Bpl.BoundVariable fVar = new Bpl.BoundVariable(tok, new Bpl.TypedIdent(tok, "$f", predef.FieldName(tok, alpha)));
      Bpl.IdentifierExpr f = new Bpl.IdentifierExpr(tok, fVar);
      
      Bpl.Expr heapOF = Bpl.Expr.SelectTok(tok, etran.HeapExpr, o, f);
      Bpl.Expr preHeapOF = Bpl.Expr.SelectTok(tok, etranPre.HeapExpr, o, f);
      Bpl.Expr ante = Bpl.Expr.And(Bpl.Expr.Neq(o, predef.Null), etran.Old.IsAlloced(tok, o));
      Bpl.Expr consequent = Bpl.Expr.Eq(heapOF, preHeapOF);
      
      consequent = Bpl.Expr.Or(consequent, InRWClause(tok, o, f, modifiesClause, etran.Old, null, null));
      
      Bpl.Trigger tr = new Bpl.Trigger(tok, true, new Bpl.ExprSeq(heapOF));
      return new Bpl.ForallExpr(tok, new Bpl.TypeVariableSeq(alpha), new Bpl.VariableSeq(oVar, fVar), null, tr, Bpl.Expr.Imp(ante, consequent));
    }
    
    // ----- Type ---------------------------------------------------------------------------------
    
    Bpl.Type TrType(Type type)
     {
      Contract.Requires(type != null);Contract.Requires( predef != null);
      Contract.Ensures(Contract.Result<Bpl.Type>() != null);

     
      while (true) {
        TypeProxy tp = type as TypeProxy;
        if (tp == null) {
          break;
        } else if (tp.T == null) {
          // unresolved proxy; just treat as ref, since no particular type information is apparently needed for this type
          return predef.RefType;
        } else {
          type = tp.T;
        }
      }
      
      if (type is BoolType) {
        return Bpl.Type.Bool;
      } else if (type is IntType) {
        return Bpl.Type.Int;
      } else if (type.IsTypeParameter) {
        return predef.BoxType;
      } else if (type.IsRefType) {
        // object and class types translate to ref
        return predef.RefType;
      } else if (type.IsDatatype) {
        return predef.DatatypeType;
      } else if (type is SetType) {
        return predef.SetType(Token.NoToken, predef.BoxType);
      } else if (type is SeqType) {
        return predef.SeqType(Token.NoToken, predef.BoxType);
      } else {
        Contract.Assert(false); throw new cce.UnreachableException();  // unexpected type
      }
    }
    
    Bpl.TypeVariableSeq TrTypeParamDecls(List<TypeParameter/*!*/>/*!*/ tps)
    {
      Contract.Requires(cce.NonNullElements(tps));
      Contract.Ensures(Contract.Result<Bpl.TypeVariableSeq>() != null);

      Bpl.TypeVariableSeq typeParams = new Bpl.TypeVariableSeq();
      return typeParams;
    }

    // ----- Statement ----------------------------------------------------------------------------
    
    Bpl.AssertCmd Assert(Bpl.IToken tok, Bpl.Expr condition, string errorMessage)
    {
      Contract.Requires(tok != null);
      Contract.Requires(condition != null);
      Contract.Requires(errorMessage != null);
      Contract.Ensures(Contract.Result<Bpl.AssertCmd>() != null);

      Bpl.AssertCmd cmd = new Bpl.AssertCmd(tok, condition);
      cmd.ErrorData = "Error: " + errorMessage;
      return cmd;
    }

    Bpl.AssertCmd AssertNS(Bpl.IToken tok, Bpl.Expr condition, string errorMessage)
    {
      Contract.Requires(tok != null);Contract.Requires(errorMessage != null);Contract.Requires(condition != null);Contract.Ensures(Contract.Result<Bpl.AssertCmd>() != null);

      List<object> args = new List<object>();
      args.Add(Bpl.Expr.Literal(0));
      Bpl.QKeyValue kv = new Bpl.QKeyValue(tok, "subsumption", args, null);
      Bpl.AssertCmd cmd = new Bpl.AssertCmd(tok, condition, kv);
      cmd.ErrorData = "Error: " + errorMessage;
      return cmd;
    }

    Bpl.Ensures Ensures(IToken tok, bool free, Bpl.Expr condition, string errorMessage, string comment)
    {
      Contract.Requires(tok != null);
      Contract.Requires(condition != null);
      Contract.Ensures(Contract.Result<Bpl.Ensures>() != null);

      Bpl.Ensures ens = new Bpl.Ensures(tok, free, condition, comment);
      if (errorMessage != null) {
        ens.ErrorData = errorMessage;
      }
      return ens;
    }

    Bpl.Requires Requires(IToken tok, bool free, Bpl.Expr condition, string errorMessage, string comment)
    {
      Contract.Requires(tok != null);
      Contract.Requires(condition != null);
      Bpl.Requires req = new Bpl.Requires(tok, free, condition, comment);
      if (errorMessage != null) {
        req.ErrorData = errorMessage;
      }
      return req;
    }

    Bpl.StmtList TrStmt2StmtList(Statement block, Bpl.VariableSeq locals, ExpressionTranslator etran)
     {
      Contract.Requires(block != null);
      Contract.Requires(locals != null);
      Contract.Requires(etran != null);Contract.Requires( currentMethod != null && predef != null);
      Contract.Ensures(Contract.Result<Bpl.StmtList>() != null);

     
      return TrStmt2StmtList(new Bpl.StmtListBuilder(), block, locals, etran);
    }
    
    Bpl.StmtList TrStmt2StmtList(Bpl.StmtListBuilder builder, Statement block, Bpl.VariableSeq locals, ExpressionTranslator etran)
     {
      Contract.Requires(builder != null);
      Contract.Requires(block != null);
      Contract.Requires(locals != null);
      Contract.Requires(etran != null);
      Contract.Requires( currentMethod != null && predef != null);
      Contract.Ensures(Contract.Result<Bpl.StmtList>() != null);

      
      TrStmt(block, builder, locals, etran);
      return builder.Collect(block.Tok);  // TODO: would be nice to have an end-curly location for "block"
    }
    
    void TrStmt(Statement stmt, Bpl.StmtListBuilder builder, Bpl.VariableSeq locals, ExpressionTranslator etran)
    {
      Contract.Requires(stmt != null);
      Contract.Requires(builder != null);
      Contract.Requires(locals != null);
      Contract.Requires(etran != null);
      Contract.Requires( currentMethod != null && predef != null);
      if (stmt is AssertStmt) {
        AddComment(builder, stmt, "assert statement");
        AssertStmt s = (AssertStmt)stmt;
        builder.Add(AssertNS(s.Expr.tok, IsTotal(s.Expr, etran), "assert condition must be well defined"));  // totality check
        List<Expression> definitions, pieces;
        if (!SplitExpr(s.Expr, out definitions, out pieces)) {
          builder.Add(Assert(s.Expr.tok, etran.TrExpr(s.Expr), "assertion violation"));
        } else {
          foreach (Expression p in definitions) {
            builder.Add(new Bpl.AssumeCmd(p.tok, etran.TrExpr(p)));
          }
          foreach (Expression p in pieces) {
            builder.Add(AssertNS(p.tok, etran.TrExpr(p), "assertion violation"));
          }
          builder.Add(new Bpl.AssumeCmd(stmt.Tok, etran.TrExpr(s.Expr)));
        }
      } else if (stmt is AssumeStmt) {
        AddComment(builder, stmt, "assume statement");
        AssumeStmt s = (AssumeStmt)stmt;
        builder.Add(AssertNS(stmt.Tok, IsTotal(s.Expr, etran), "assume condition must be well defined"));  // totality check
        builder.Add(new Bpl.AssumeCmd(stmt.Tok, etran.TrExpr(s.Expr)));
      } else if (stmt is UseStmt) {
        AddComment(builder, stmt, "use statement");
        UseStmt s = (UseStmt)stmt;
        // Skip the totality check.  This makes the 'use' statement easier to use and it has no executable analog anyhow
        // builder.Add(Assert(stmt.Tok, IsTotal(s.Expr, etran), "use expression must be well defined"));  // totality check
        builder.Add(new Bpl.AssumeCmd(stmt.Tok, (s.EvalInOld ? etran.Old : etran).TrUseExpr(s.FunctionCallExpr)));
      } else if (stmt is PrintStmt) {
        AddComment(builder, stmt, "print statement");
        PrintStmt s = (PrintStmt)stmt;
        foreach (Attributes.Argument arg in s.Args) {
          if (arg.E != null) {
            builder.Add(AssertNS(stmt.Tok, IsTotal(arg.E, etran), "print expression must be well defined"));  // totality check
          }
        }
        
      } else if (stmt is LabelStmt) {
        AddComment(builder, stmt, "label statement");  // TODO: ouch, comments probably mess up what the label labels in the Boogie program
        builder.AddLabelCmd(((LabelStmt)stmt).Label);
      } else if (stmt is BreakStmt) {
        AddComment(builder, stmt, "break statement");
        builder.Add(new Bpl.BreakCmd(stmt.Tok, ((BreakStmt)stmt).TargetLabel));  // TODO: handle name clashes of labels
      } else if (stmt is ReturnStmt) {
        AddComment(builder, stmt, "return statement");
        builder.Add(new Bpl.ReturnCmd(stmt.Tok));
      } else if (stmt is AssignStmt) {
        AddComment(builder, stmt, "assignment statement");
        AssignStmt s = (AssignStmt)stmt;
        TrAssignment(stmt.Tok, s.Lhs, s.Rhs, builder, locals, etran);
        if (s.Lhs is IdentifierExpr) {
          Bpl.IdentifierExpr v = etran.TrVar(stmt.Tok, cce.NonNull((IdentifierExpr)s.Lhs).Var);
          builder.Add(new Bpl.CallCmd(stmt.Tok, "CevUpdate",
            new Bpl.ExprSeq(CevLocation(stmt.Tok), CevVariable(stmt.Tok, v.Name), v),
            new Bpl.IdentifierExprSeq()));
        } else if (s.Lhs is SeqSelectExpr) {
          // TODO: CEV
        } else {
          builder.Add(new Bpl.CallCmd(stmt.Tok, "CevUpdateHeap",
            new Bpl.ExprSeq(CevLocation(stmt.Tok), CevVariable(stmt.Tok, predef.HeapVarName), etran.HeapExpr),
            new Bpl.IdentifierExprSeq()));
        }
      } else if (stmt is VarDecl) {
        AddComment(builder, stmt, "var-declaration statement");
        VarDecl s = (VarDecl)stmt;
        Bpl.Type varType = TrType(s.Type);
        Bpl.Expr wh = GetWhereClause(stmt.Tok, new Bpl.IdentifierExpr(stmt.Tok, s.UniqueName, varType), s.Type, etran);
        Bpl.Expr cevWh = FunctionCall(stmt.Tok, BuiltinFunction.CevVarUpdate, null,
          Bpl.Expr.Ident("#cev_pc", Bpl.Type.Int),
          new Bpl.IdentifierExpr(stmt.Tok, "cev_local", predef.CevVariableKind),
          CevVariable(stmt.Tok, s.UniqueName),
          new Bpl.IdentifierExpr(stmt.Tok, s.UniqueName, varType));
        wh = wh == null ? cevWh : Bpl.Expr.And(wh, cevWh);
        Bpl.LocalVariable var = new Bpl.LocalVariable(stmt.Tok, new Bpl.TypedIdent(stmt.Tok, s.UniqueName, varType, wh));
        locals.Add(var);
        if (s.Rhs != null) {
          IdentifierExpr ide = new IdentifierExpr(stmt.Tok, var.Name);  // allocate an expression for the assignment LHS...
          ide.Var = s;  ide.Type = s.Type;  // ... and resolve it right here
          TrAssignment(stmt.Tok, ide, s.Rhs, builder, locals, etran);
        }
        builder.Add(new Bpl.CallCmd(stmt.Tok, "CevVarIntro",
          new Bpl.ExprSeq(
            CevLocation(stmt.Tok),
            new Bpl.IdentifierExpr(stmt.Tok, "cev_local", predef.CevVariableKind),
            CevVariable(stmt.Tok, var.Name),
            new Bpl.IdentifierExpr(stmt.Tok, var)),
          new Bpl.IdentifierExprSeq()));
        
      } else if (stmt is CallStmt) {
        CallStmt s = (CallStmt)stmt;
        foreach (VarDecl local in s.NewVars) {
          TrStmt(local, builder, locals, etran);
        }
        AddComment(builder, stmt, "call statement");
        Bpl.ExprSeq ins = new Bpl.ExprSeq();
        Contract.Assert( s.Method != null);  // follows from the fact that stmt has been successfully resolved
        if (!s.Method.IsStatic) {
          ins.Add(etran.TrExpr(s.Receiver));
        }

        // Ideally, the modifies and decreases checks would be done after the precondition check,
        // but Boogie doesn't give us a hook for that.  So, we set up our own local variables here to
        // store the actual parameters.
        // Create a local variable for each formal parameter, and assign each actual parameter to the corresponding local
        Dictionary<IVariable,Expression> substMap = new Dictionary<IVariable,Expression>();
        for (int i = 0; i < s.Method.Ins.Count; i++) {
          Formal p = s.Method.Ins[i];
          VarDecl local = new VarDecl(p.tok, p.Name, p.Type, p.IsGhost, null);
          local.type = local.OptionalType;  // resolve local here
          IdentifierExpr ie = new IdentifierExpr(local.Tok, local.UniqueName);
          ie.Var = local;  ie.Type = ie.Var.Type;  // resolve ie here
          substMap.Add(p, ie);
          locals.Add(new Bpl.LocalVariable(local.Tok, new Bpl.TypedIdent(local.Tok, local.UniqueName, TrType(local.Type))));
          
          Bpl.IdentifierExpr lhs = (Bpl.IdentifierExpr)etran.TrExpr(ie);  // TODO: is this cast always justified?
          Expression actual = s.Args[i];
          builder.Add(Assert(actual.tok, IsTotal(actual, etran), "argument must be well defined"));  // totality check
          Bpl.Cmd cmd = Bpl.Cmd.SimpleAssign(p.tok, lhs, etran.CondApplyBox(stmt.Tok, etran.TrExpr(actual), cce.NonNull(actual.Type), s.Method.Ins[i].Type));
          builder.Add(cmd);
          ins.Add(lhs);
        }
        // Also create variables to hold the output parameters of the call, so that appropriate unboxes can be introduced.
        Bpl.IdentifierExprSeq outs = new Bpl.IdentifierExprSeq();
        List<Bpl.IdentifierExpr> tmpOuts = new List<Bpl.IdentifierExpr>(s.Lhs.Count);
        for (int i = 0; i < s.Lhs.Count; i++) {
          Expression e = s.Lhs[i];
          if (ExpressionTranslator.ModeledAsBoxType(s.Method.Outs[i].Type) && !ExpressionTranslator.ModeledAsBoxType(cce.NonNull(e.Type))) {
            // we need an Unbox
            Bpl.LocalVariable var = new Bpl.LocalVariable(stmt.Tok, new Bpl.TypedIdent(stmt.Tok, "$tmp#" + otherTmpVarCount, predef.BoxType));
            otherTmpVarCount++;
            locals.Add(var);
            Bpl.IdentifierExpr varIdE = new Bpl.IdentifierExpr(stmt.Tok, var.Name, predef.BoxType);
            tmpOuts.Add(varIdE);
            outs.Add(varIdE);
          } else {
            tmpOuts.Add(null);
            outs.Add(etran.TrExpr(e));
          }
        }

        // Check modifies clause
        CheckFrameSubset(stmt.Tok, s.Method.Mod, s.Receiver, substMap, etran, builder, "call may violate caller's modifies clause");

        // Check termination
        ModuleDecl module = cce.NonNull(s.Method.EnclosingClass).Module;
        if (module == cce.NonNull(currentMethod.EnclosingClass).Module) {
          if (module.CallGraph.GetSCCRepresentative(s.Method) == module.CallGraph.GetSCCRepresentative(currentMethod)) {
            CheckCallTermination(stmt.Tok, currentMethod.Decreases, s.Method.Decreases, s.Receiver, substMap, etran, builder);
          }
        }

        // Make the call
        Bpl.CallCmd call = new Bpl.CallCmd(stmt.Tok, s.Method.FullName, ins, outs);
        builder.Add(call);
        for (int i = 0; i < s.Lhs.Count; i++) {
          Bpl.IdentifierExpr tmpVarIdE = tmpOuts[i];
          IdentifierExpr e = s.Lhs[i];
          Bpl.IdentifierExpr lhs = (Bpl.IdentifierExpr)etran.TrExpr(e);  // TODO: is this cast always justified?
          if (tmpVarIdE != null) {
            // e := UnBox(tmpVar);
            Bpl.Cmd cmd = Bpl.Cmd.SimpleAssign(stmt.Tok, lhs, FunctionCall(stmt.Tok, BuiltinFunction.Unbox, TrType(cce.NonNull(e.Type)), tmpVarIdE));
            builder.Add(cmd);
          }
          builder.Add(new Bpl.CallCmd(stmt.Tok, "CevUpdateHere",
            new Bpl.ExprSeq(CevVariable(stmt.Tok, e.Name), lhs),
            new Bpl.IdentifierExprSeq()));
        }
        builder.Add(new Bpl.CallCmd(stmt.Tok, "CevStep", new Bpl.ExprSeq(CevLocation(stmt.Tok)), new Bpl.IdentifierExprSeq()));
        
      } else if (stmt is BlockStmt) {
        foreach (Statement ss in ((BlockStmt)stmt).Body) {
          TrStmt(ss, builder, locals, etran);
        }
      } else if (stmt is IfStmt) {
        AddComment(builder, stmt, "if statement");
        IfStmt s = (IfStmt)stmt;
        builder.Add(new Bpl.CallCmd(s.Tok, "CevEvent",
          new Bpl.ExprSeq(CevLocation(s.Tok), new Bpl.IdentifierExpr(s.Tok, "conditional_moment", predef.CevEventType)),
          new Bpl.IdentifierExprSeq()));
        Bpl.Expr guard;
        if (s.Guard == null) {
          guard = null;
        } else {
          builder.Add(Assert(s.Guard.tok, IsTotal(s.Guard, etran), "if guard must be well defined"));  // totality check
          guard = etran.TrExpr(s.Guard);
        }
        Bpl.StmtListBuilder b = new Bpl.StmtListBuilder();
        b.Add(new Bpl.CallCmd(s.Thn.Tok, "CevEvent",
          new Bpl.ExprSeq(CevLocation(s.Thn.Tok), new Bpl.IdentifierExpr(s.Thn.Tok, "took_then_branch", predef.CevEventType)),
          new Bpl.IdentifierExprSeq()));
        Bpl.StmtList thn = TrStmt2StmtList(b, s.Thn, locals, etran);
        Bpl.StmtList els;
        Bpl.IfCmd elsIf = null;
        if (s.Els == null) {
          b = new Bpl.StmtListBuilder();
          b.Add(new Bpl.CallCmd(s.Tok, "CevEvent",
            new Bpl.ExprSeq(CevLocation(s.Tok), new Bpl.IdentifierExpr(s.Tok, "took_else_branch", predef.CevEventType)),
            new Bpl.IdentifierExprSeq()));
          els = b.Collect(s.Tok);
        } else {
          b = new Bpl.StmtListBuilder();
          b.Add(new Bpl.CallCmd(s.Els.Tok, "CevEvent",
            new Bpl.ExprSeq(CevLocation(s.Els.Tok), new Bpl.IdentifierExpr(s.Els.Tok, "took_else_branch", predef.CevEventType)),
            new Bpl.IdentifierExprSeq()));
          els = TrStmt2StmtList(b, s.Els, locals, etran);
          if (els.BigBlocks.Count == 1) {
            Bpl.BigBlock bb = els.BigBlocks[0];
            if (bb.LabelName == null && bb.simpleCmds.Length == 0 && bb.ec is Bpl.IfCmd) {
              elsIf = (Bpl.IfCmd)bb.ec;
              els = null;
            }
          }
        }
        builder.Add(new Bpl.IfCmd(stmt.Tok, guard, thn, elsIf, els));
        
      } else if (stmt is WhileStmt) {
        AddComment(builder, stmt, "while statement");
        WhileStmt s = (WhileStmt)stmt;
        int loopId = loopHeapVarCount;
        loopHeapVarCount++;
        
        // use simple heuristics to create a default decreases clause, if none is given
        List<Expression> theDecreases = s.Decreases;
        bool inferredDecreases = false;
        if (theDecreases.Count == 0 && s.Guard != null) {
          Expression prefix = null;
          foreach (Expression guardConjunct in Conjuncts(s.Guard)) {
            Expression guess = null;
            BinaryExpr bin = guardConjunct as BinaryExpr;
            if (bin != null) {
              switch (bin.ResolvedOp) {
                case BinaryExpr.ResolvedOpcode.Lt:
                case BinaryExpr.ResolvedOpcode.Le:
                  // for A < B and A <= B, use the decreases B - A
                  guess = CreateIntSub(s.Tok, bin.E1, bin.E0);
                  break;
                case BinaryExpr.ResolvedOpcode.Ge:
                case BinaryExpr.ResolvedOpcode.Gt:
                  // for A >= B and A > B, use the decreases A - B
                  guess = CreateIntSub(s.Tok, bin.E0, bin.E1);
                  break;
                case BinaryExpr.ResolvedOpcode.NeqCommon:
                  if (bin.E0.Type is IntType) {
                    // for A != B where A and B are integers, use the absolute difference between A and B (that is: if 0 <= A-B then A-B else B-A)
                    Expression AminusB = CreateIntSub(s.Tok, bin.E0, bin.E1);
                    Expression BminusA = CreateIntSub(s.Tok, bin.E1, bin.E0);
                    Expression zero = CreateIntLiteral(s.Tok, 0);
                    BinaryExpr test = new BinaryExpr(s.Tok, BinaryExpr.Opcode.Le, zero, AminusB);
                    test.ResolvedOp = BinaryExpr.ResolvedOpcode.Le;  // resolve here
                    test.Type = Type.Bool;  // resolve here
                    guess = CreateIntITE(s.Tok, test, AminusB, BminusA);
                  }
                  break;
                default:
                  break;
              }
            }
            if (guess != null) {
              if (prefix != null) {
                // Make the following guess:  if prefix then guess else -1
                Expression negativeOne = CreateIntLiteral(s.Tok, -1);
                guess = CreateIntITE(s.Tok, prefix, guess, negativeOne);
              }
              theDecreases.Add(guess);
              inferredDecreases = true;
              break;  // ignore any further conjuncts
            }
            if (prefix == null) {
              prefix = guardConjunct;
            } else {
              BinaryExpr and = new BinaryExpr(s.Tok, BinaryExpr.Opcode.And, prefix, guardConjunct);
              and.ResolvedOp = BinaryExpr.ResolvedOpcode.And;  // resolve here
              and.Type = Type.Bool;  // resolve here
              prefix = and;
            }
          }
        }
        
        Bpl.LocalVariable preLoopHeapVar = new Bpl.LocalVariable(stmt.Tok, new Bpl.TypedIdent(stmt.Tok, "$PreLoopHeap" + loopId, predef.HeapType));
        locals.Add(preLoopHeapVar);
        Bpl.IdentifierExpr preLoopHeap = new Bpl.IdentifierExpr(stmt.Tok, preLoopHeapVar);
        ExpressionTranslator etranPreLoop = new ExpressionTranslator(this, predef, preLoopHeap);
        builder.Add(Bpl.Cmd.SimpleAssign(stmt.Tok, preLoopHeap, etran.HeapExpr));  // TODO: does this screw up labeled breaks for this loop?

        List<Bpl.Expr> initDecr = null;
        if (!Contract.Exists(theDecreases,e=> e is WildcardExpr)) {
          initDecr = RecordDecreasesValue(theDecreases, builder, locals, etran, "$decr" + loopId + "$init$");
        }

        // the variable w is used to coordinate the definedness checking of the loop invariant
        Bpl.LocalVariable wVar = new Bpl.LocalVariable(stmt.Tok, new Bpl.TypedIdent(stmt.Tok, "$w" + loopId, Bpl.Type.Bool));
        Bpl.IdentifierExpr w = new Bpl.IdentifierExpr(stmt.Tok, wVar);
        locals.Add(wVar);
        // havoc w;
        builder.Add(new Bpl.HavocCmd(stmt.Tok, new Bpl.IdentifierExprSeq(w)));

        // CEV information
        Bpl.LocalVariable preLoopCevPCVar = new Bpl.LocalVariable(stmt.Tok, new Bpl.TypedIdent(stmt.Tok, "$PreLoopCevPC" + loopId, Bpl.Type.Int));
        locals.Add(preLoopCevPCVar);
        Bpl.IdentifierExpr preLoopCevPC = new Bpl.IdentifierExpr(stmt.Tok, preLoopCevPCVar);
        // call preLoopCevPC := CevPreLoop(loc);
        builder.Add(new Bpl.CallCmd(stmt.Tok, "CevPreLoop",
          new Bpl.ExprSeq(CevLocation(stmt.Tok)),
          new Bpl.IdentifierExprSeq(preLoopCevPC)));  // TODO: does this screw up labeled breaks for this loop?
        
        List<Bpl.PredicateCmd> invariants = new List<Bpl.PredicateCmd>();
        Bpl.StmtListBuilder invDefinednessBuilder = new Bpl.StmtListBuilder();
        foreach (MaybeFreeExpression loopInv in s.Invariants) {
          invDefinednessBuilder.Add(AssertNS(loopInv.E.tok, IsTotal(loopInv.E, etran), (loopInv.IsFree ? "free " : "") + "loop invariant must be well defined"));  // totality check
          invDefinednessBuilder.Add(new Bpl.AssumeCmd(loopInv.E.tok, etran.TrExpr(loopInv.E)));
          if (loopInv.IsFree) {
            invariants.Add(new Bpl.AssumeCmd(loopInv.E.tok, Bpl.Expr.Imp(w, etran.TrExpr(loopInv.E))));
          } else {
            List<Expression> definitions, pieces;
            if (!SplitExpr(loopInv.E, out definitions, out pieces)) {
              invariants.Add(Assert(loopInv.E.tok, Bpl.Expr.Imp(w, etran.TrExpr(loopInv.E)), "loop invariant violation"));
            } else {
              Bpl.Expr ante = w;
              foreach (Expression d in definitions) {
                Bpl.Expr trD = etran.TrExpr(d);
                invariants.Add(new Bpl.AssumeCmd(d.tok, trD));
                ante = Bpl.Expr.And(ante, trD);
              }
              foreach (Expression se in pieces) {
                invariants.Add(Assert(se.tok, Bpl.Expr.Imp(ante, etran.TrExpr(se)), "loop invariant violation"));  // TODO: it would be fine to have this use {:subsumption 0}
              }
              invariants.Add(new Bpl.AssumeCmd(loopInv.E.tok, Bpl.Expr.Imp(w, etran.TrExpr(loopInv.E))));
            }
          }
        }
        // check definedness of decreases clause
        foreach (Expression e in theDecreases) {
          invDefinednessBuilder.Add(Assert(e.tok, IsTotal(e, etran), "decreases expression must be well defined at top of each loop iteration"));  // totality check
        }
        // include boilerplate invariants
        foreach (BoilerplateTriple tri in GetTwoStateBoilerplate(stmt.Tok, currentMethod, etranPreLoop, etran)) {
          if (tri.IsFree) {
            invariants.Add(new Bpl.AssumeCmd(stmt.Tok, tri.Expr));
          } else {
            Contract.Assert( tri.ErrorMessage != null);  // follows from BoilerplateTriple invariant
            invariants.Add(Assert(stmt.Tok, tri.Expr, tri.ErrorMessage));
          }
        }
        // include a free invariant that says that all completed iterations so far have only decreased the termination metric
        if (initDecr != null) {
          List<IToken> toks = new List<IToken>();
          List<Type> types = new List<Type>();
          List<Bpl.Expr> decrs = new List<Bpl.Expr>();
          foreach (Expression e in theDecreases) {
            toks.Add(e.tok);
            types.Add(cce.NonNull(e.Type));
            decrs.Add(etran.TrExpr(e));
          }
          Bpl.Expr decrCheck = DecreasesCheck(toks, types, decrs, initDecr, etran, null, null, true);
          invariants.Add(new Bpl.AssumeCmd(stmt.Tok, decrCheck));
        }
        
        // CEV information
        invariants.Add(new Bpl.AssumeCmd(stmt.Tok, Bpl.Expr.Eq(FunctionCall(stmt.Tok, BuiltinFunction.CevProgramLocation, null, Bpl.Expr.Ident("#cev_pc", Bpl.Type.Int)), CevLocation(stmt.Tok))));
        invariants.Add(new Bpl.AssumeCmd(stmt.Tok, Bpl.Expr.Lt(preLoopCevPC, Bpl.Expr.Ident("#cev_pc", Bpl.Type.Int))));
        invariants.Add(new Bpl.AssumeCmd(stmt.Tok, FunctionCall(stmt.Tok, BuiltinFunction.CevControlFlowEvent,
          null,
          Bpl.Expr.Ident("#cev_pc", Bpl.Type.Int),
          new Bpl.IdentifierExpr(stmt.Tok, "loop_register", predef.CevEventType))));
        foreach (Formal p in currentMethod.Outs) {
          Bpl.Expr wh = FunctionCall(stmt.Tok, BuiltinFunction.CevVarUpdate, null,
            Bpl.Expr.Ident("#cev_pc", Bpl.Type.Int),
            new Bpl.IdentifierExpr(stmt.Tok, "cev_local", predef.CevVariableKind),
            CevVariable(stmt.Tok, p.UniqueName),
            etran.TrVar(stmt.Tok, p));
          invariants.Add(new Bpl.AssumeCmd(stmt.Tok, wh));
        }

        Bpl.StmtListBuilder loopBodyBuilder = new Bpl.StmtListBuilder();
        // as the first thing inside the loop, generate:  if (!w) { assert IsTotal(inv); assume false; }
        invDefinednessBuilder.Add(new Bpl.AssumeCmd(stmt.Tok, Bpl.Expr.False));
        loopBodyBuilder.Add(new Bpl.IfCmd(stmt.Tok, Bpl.Expr.Not(w), invDefinednessBuilder.Collect(stmt.Tok), null, null));
        // generate:  Contract.Assert( IsTotal(guard); if (!guard) { break); }
        Bpl.Expr guard;
        if (s.Guard == null) {
          guard = null;
        } else {
          loopBodyBuilder.Add(Assert(s.Guard.tok, IsTotal(s.Guard, etran), "loop guard must be well defined"));  // totality check
          Bpl.StmtListBuilder guardBreak = new Bpl.StmtListBuilder();
          guardBreak.Add(new Bpl.BreakCmd(s.Guard.tok, null));
          loopBodyBuilder.Add(new Bpl.IfCmd(s.Guard.tok, Bpl.Expr.Not(etran.TrExpr(s.Guard)), guardBreak.Collect(s.Guard.tok), null, null));
          guard = Bpl.Expr.True;
        }
        // CEV information
        loopBodyBuilder.Add(new Bpl.CallCmd(stmt.Tok, "CevStepEvent",
          new Bpl.ExprSeq(CevLocation(stmt.Tok), new Bpl.IdentifierExpr(stmt.Tok, "loop_entered", predef.CevEventType)),
          new Bpl.IdentifierExprSeq()));
        // termination checking
        if (Contract.Exists(theDecreases,e=> e is WildcardExpr)) {
          // omit termination checking for this loop
          TrStmt(s.Body, loopBodyBuilder, locals, etran);
        } else {
          List<Bpl.Expr> oldBfs = RecordDecreasesValue(theDecreases, loopBodyBuilder, locals, etran, "$decr" + loopId + "$");
          // time for the actual loop body
          TrStmt(s.Body, loopBodyBuilder, locals, etran);
          // check definedness of decreases expressions
          List<IToken> toks = new List<IToken>();
          List<Type> types = new List<Type>();
          List<Bpl.Expr> decrs = new List<Bpl.Expr>();
          foreach (Expression e in theDecreases) {
            toks.Add(e.tok);
            types.Add(cce.NonNull(e.Type));
            decrs.Add(etran.TrExpr(e));
          }
          Bpl.Expr decrCheck = DecreasesCheck(toks, types, decrs, oldBfs, etran, loopBodyBuilder, " at end of loop iteration", false);
          loopBodyBuilder.Add(Assert(stmt.Tok, decrCheck, inferredDecreases ? "cannot prove termination; try supplying a decreases clause for the loop" : "decreases expression might not decrease"));
        }
        Bpl.StmtList body = loopBodyBuilder.Collect(stmt.Tok);

        builder.Add(new Bpl.WhileCmd(stmt.Tok, guard, invariants, body));
        builder.Add(new Bpl.CallCmd(stmt.Tok, "CevStepEvent",
          new Bpl.ExprSeq(CevLocation(stmt.Tok), new Bpl.IdentifierExpr(stmt.Tok, "loop_exited", predef.CevEventType)),
          new Bpl.IdentifierExprSeq()));

      } else if (stmt is ForeachStmt) {
        AddComment(builder, stmt, "foreach statement");
        ForeachStmt s = (ForeachStmt)stmt;
        // assert/assume (forall o: ref ::  o != null && o in S && Range(o) ==> Expr);
        // assert (forall o: ref :: o != null && o in S && Range(o)  ==> IsTotal(RHS));
        // assert (forall o: ref :: o != null && o in S && Range(o)  ==> $_Frame[o,F]);  // this checks the enclosing modifies clause
        // var oldHeap := $Heap;
        // havoc $Heap;
        // assume $HeapSucc(oldHeap, $Heap);
        // assume (forall<alpha> o: ref, f: Field alpha ::  $Heap[o,f] = oldHeap[o,f] || (f = F && o != null && o in S && Range(o)));
        // assume (forall o: ref ::  o != null && o in S && Range(o) ==> $Heap[o,F] =  RHS[$Heap := oldHeap]);
        // Note, $Heap[o,alloc] is intentionally omitted from the antecedent of the quantifier in the previous line.  That
        // allocatedness property should hold automatically, because the set/seq quantified is a program expression, which
        // will have been constructed from allocated objects.
        // For sets, "o in S" means just that.  For sequences, "o in S" is:
        //   (exists i :: { Seq#Index(S,i) }  0 <= i && i < Seq#Length(S) && Seq#Index(S,i) == o)

        Bpl.BoundVariable oVar = new Bpl.BoundVariable(stmt.Tok, new Bpl.TypedIdent(stmt.Tok, s.BoundVar.UniqueName, TrType(s.BoundVar.Type)));
        Bpl.IdentifierExpr o = new Bpl.IdentifierExpr(stmt.Tok, oVar);

        // colection
        builder.Add(Assert(s.Collection.tok, IsTotal(s.Collection, etran), "collection expression must be well defined"));
        Bpl.Expr oInS;
        if (s.Collection.Type is SetType) {
          oInS = etran.TrInSet(stmt.Tok, o, s.Collection, ((SetType)s.Collection.Type).Arg);
        } else {
          Bpl.BoundVariable iVar = new Bpl.BoundVariable(stmt.Tok, new Bpl.TypedIdent(stmt.Tok, "$i", Bpl.Type.Int));
          Bpl.IdentifierExpr i = new Bpl.IdentifierExpr(stmt.Tok, iVar);
          Bpl.Expr S = etran.TrExpr(s.Collection);
          Bpl.Expr range = InSeqRange(stmt.Tok, i, S, true, null, false);
          Bpl.Expr Si = FunctionCall(stmt.Tok, BuiltinFunction.SeqIndex, predef.BoxType, S, i);
          Bpl.Trigger tr = new Bpl.Trigger(stmt.Tok, true, new Bpl.ExprSeq(Si));
          // TODO: in the next line, the == should be replaced by something that understands extensionality, for sets and sequences
          oInS = new Bpl.ExistsExpr(stmt.Tok, new Bpl.VariableSeq(iVar), tr, Bpl.Expr.And(range, Bpl.Expr.Eq(Si, o)));
        }
        oInS = Bpl.Expr.And(Bpl.Expr.Neq(o, predef.Null), oInS);
        
        // range
        Bpl.Expr qr = new Bpl.ForallExpr(s.Range.tok, new Bpl.VariableSeq(oVar), Bpl.Expr.Imp(oInS, IsTotal(s.Range, etran)));
        builder.Add(AssertNS(s.Range.tok, qr, "range expression must be well defined"));
        oInS = Bpl.Expr.And(oInS, etran.TrExpr(s.Range));

        // sequence of asserts and assumes and uses
        foreach (PredicateStmt ps in s.BodyPrefix) {
          if (ps is AssertStmt) {
            Bpl.Expr q = new Bpl.ForallExpr(ps.Expr.tok, new Bpl.VariableSeq(oVar), Bpl.Expr.Imp(oInS, IsTotal(ps.Expr, etran)));
            builder.Add(AssertNS(ps.Expr.tok, q, "assert condition must be well defined"));  // totality check
            List<Expression> definitions, pieces;
            SplitExpr(ps.Expr, out definitions, out pieces);
            foreach (Expression d in definitions) {
              Bpl.Expr e = etran.TrExpr(d);
              q = new Bpl.ForallExpr(d.tok, new Bpl.VariableSeq(oVar), Bpl.Expr.Imp(oInS, e));
              builder.Add(new Bpl.AssumeCmd(d.tok, q));
            }
            foreach (Expression se in pieces) {
              Bpl.Expr e = etran.TrExpr(se);
              q = new Bpl.ForallExpr(se.tok, new Bpl.VariableSeq(oVar), Bpl.Expr.Imp(oInS, e));
              builder.Add(Assert(se.tok, q, "assertion violation"));  // TODO: it would be a fine idea to let this use {:subsumption 0}
            }
          } else if (ps is AssumeStmt) {
            Bpl.Expr eIsTotal = IsTotal(ps.Expr, etran);
            Bpl.Expr q = new Bpl.ForallExpr(ps.Expr.tok, new Bpl.VariableSeq(oVar), Bpl.Expr.Imp(oInS, eIsTotal));
            builder.Add(AssertNS(ps.Expr.tok, q, "assume condition must be well defined"));  // totality check
          } else {
            Contract.Assert( ps is UseStmt);
            // no totality check (see UseStmt case above)
          }
          Bpl.Expr enchilada;  // the whole enchilada
          if (ps is UseStmt) {
            UseStmt us = (UseStmt)ps;
            enchilada = (us.EvalInOld ? etran.Old : etran).TrUseExpr(us.FunctionCallExpr);
          } else {
            enchilada = etran.TrExpr(ps.Expr);
          }
          Bpl.Expr qEnchilada = new Bpl.ForallExpr(ps.Expr.tok, new Bpl.VariableSeq(oVar), Bpl.Expr.Imp(oInS, enchilada));
          builder.Add(new Bpl.AssumeCmd(ps.Expr.tok, qEnchilada));
        }

        // Check RHS of assignment to be well defined
        ExprRhs rhsExpr = s.BodyAssign.Rhs as ExprRhs;
        if (rhsExpr != null) {
          // assert (forall o: ref :: o != null && o in S && Range(o) ==> IsTotal(RHS));
          Bpl.Expr bbb = Bpl.Expr.Imp(oInS, IsTotal(rhsExpr.Expr, etran));
          Bpl.Expr qqq = new Bpl.ForallExpr(stmt.Tok, new Bpl.VariableSeq(oVar), bbb);
          builder.Add(AssertNS(rhsExpr.Expr.tok, qqq, "RHS of assignment must be well defined"));  // totality check
        }
              
        // Here comes:  Contract.Assert( (forall o: ref :: o != null && o in S && Range(o) ==> $_Frame[o,F]);
        Bpl.Expr body = Bpl.Expr.Imp(oInS, Bpl.Expr.Select(etran.TheFrame(stmt.Tok), o, GetField((FieldSelectExpr)s.BodyAssign.Lhs)));
        Bpl.Expr qq = new Bpl.ForallExpr(stmt.Tok, new Bpl.VariableSeq(oVar), body);
        builder.Add(Assert(s.BodyAssign.Tok, qq, "foreach assignment may update an object not in the enclosing method's modifies clause"));

        // Set up prevHeap
        Bpl.IdentifierExpr prevHeap = GetPrevHeapVar_IdExpr(stmt.Tok, locals);
        builder.Add(Bpl.Cmd.SimpleAssign(stmt.Tok, prevHeap, etran.HeapExpr));
        builder.Add(new Bpl.HavocCmd(stmt.Tok, new Bpl.IdentifierExprSeq((Bpl.IdentifierExpr/*TODO: this cast is rather dubious*/)etran.HeapExpr)));
        builder.Add(new Bpl.AssumeCmd(stmt.Tok, FunctionCall(stmt.Tok, BuiltinFunction.HeapSucc, null, prevHeap, etran.HeapExpr)));

        // Here comes:  assume (forall<alpha> o: ref, f: Field alpha ::  $Heap[o,f] = oldHeap[o,f] || (f = F && o != null && o in S && Range(o)));
        Bpl.TypeVariable alpha = new Bpl.TypeVariable(stmt.Tok, "alpha");
        Bpl.BoundVariable fVar = new Bpl.BoundVariable(stmt.Tok, new Bpl.TypedIdent(stmt.Tok, "$f", predef.FieldName(stmt.Tok, alpha)));
        Bpl.IdentifierExpr f = new Bpl.IdentifierExpr(stmt.Tok, fVar);
        Bpl.Expr heapOF = Bpl.Expr.SelectTok(stmt.Tok, etran.HeapExpr, o, f);
        Bpl.Expr oldHeapOF = Bpl.Expr.SelectTok(stmt.Tok, prevHeap, o, f);
        body = Bpl.Expr.Or(
          Bpl.Expr.Eq(heapOF, oldHeapOF),
          Bpl.Expr.And(
            Bpl.Expr.Eq(f, GetField((FieldSelectExpr)s.BodyAssign.Lhs)),
            oInS));
        qq = new Bpl.ForallExpr(stmt.Tok, new Bpl.TypeVariableSeq(alpha), new Bpl.VariableSeq(oVar, fVar), body);
        builder.Add(new Bpl.AssumeCmd(stmt.Tok, qq));

        // Here comes:  assume (forall o: ref ::  o != null && o in S && Range(o) ==> $Heap[o,F] =  RHS[$Heap := oldHeap]);
        if (rhsExpr != null) {
          Bpl.Expr heapOField = Bpl.Expr.SelectTok(stmt.Tok, etran.HeapExpr, o, GetField((FieldSelectExpr)(s.BodyAssign).Lhs));
          ExpressionTranslator oldEtran = new ExpressionTranslator(this, predef, prevHeap);
          body = Bpl.Expr.Imp(oInS, Bpl.Expr.Eq(heapOField, oldEtran.TrExpr(rhsExpr.Expr)));
          qq = new Bpl.ForallExpr(stmt.Tok, new Bpl.VariableSeq(oVar), body);
          builder.Add(new Bpl.AssumeCmd(stmt.Tok, qq));
        }
        
        // CEV information
        builder.Add(new Bpl.CallCmd(stmt.Tok, "CevUpdateHeap",
          new Bpl.ExprSeq(CevLocation(stmt.Tok), CevVariable(stmt.Tok, predef.HeapVarName), etran.HeapExpr),
          new Bpl.IdentifierExprSeq()));
        
      } else if (stmt is MatchStmt) {
        MatchStmt s = (MatchStmt)stmt;
        builder.Add(Assert(s.Source.tok, IsTotal(s.Source, etran), "match source expression must be well defined"));  // totality check
        Bpl.Expr source = etran.TrExpr(s.Source);
        
        Bpl.StmtListBuilder b = new Bpl.StmtListBuilder();
        b.Add(new Bpl.AssumeCmd(stmt.Tok, Bpl.Expr.False));
        Bpl.StmtList els = b.Collect(stmt.Tok);
        Bpl.IfCmd ifCmd = null;
        for (int i = s.Cases.Count; 0 <= --i; ) {
          MatchCaseStmt mc = (MatchCaseStmt)s.Cases[i];
          // havoc all bound variables
          List<Expression> rArgs = new List<Expression>();
          Bpl.IdentifierExprSeq havocIds = new Bpl.IdentifierExprSeq();
          foreach (BoundVar arg in mc.Arguments) {
            Bpl.LocalVariable v = new Bpl.LocalVariable(arg.tok, new Bpl.TypedIdent(arg.tok, arg.UniqueName, TrType(arg.Type)));
            locals.Add(v);
            havocIds.Add(new Bpl.IdentifierExpr(arg.tok, v));
            IdentifierExpr ie = new IdentifierExpr(arg.tok, arg.UniqueName);
            ie.Var = arg;  ie.Type = ie.Var.Type;  // resolve it here
            rArgs.Add(ie);
          }
          if (havocIds.Length != 0) {
            builder.Add(new Bpl.HavocCmd(mc.tok, havocIds));
          }
          Contract.Assert( mc.Ctor != null && mc.Ctor.EnclosingDatatype != null);  // everything has been successfully resolved
          DatatypeValue r = new DatatypeValue(mc.tok, mc.Ctor.EnclosingDatatype.Name, mc.Ctor.Name, rArgs);
          r.Ctor = mc.Ctor;  r.Type = new UserDefinedType(mc.tok, mc.Ctor.EnclosingDatatype.Name, new List<Type>()/*this is not right, but it seems like it won't matter here*/);  // resolve it here

          // translate the body into b
          b = new Bpl.StmtListBuilder();
          foreach (Statement ss in mc.Body) {
            TrStmt(ss, b, locals, etran);
          }

          Bpl.Expr guard = Bpl.Expr.Eq(source, etran.TrExpr(r));
          ifCmd = new Bpl.IfCmd(mc.tok, guard, b.Collect(mc.tok), ifCmd, els);
          els = null;
        }
        Contract.Assert( ifCmd != null);  // follows from the fact that s.Cases.Count != 0.
        builder.Add(ifCmd);

      } else {
        Contract.Assert(false); throw new cce.UnreachableException();  // unexpected statement
      }
    }
    
    static Expression CreateIntLiteral(IToken tok, int n)
    {
      Contract.Requires(tok != null);
      Contract.Ensures(Contract.Result<Expression>() != null);

      if (0 <= n) {
        Expression lit = new LiteralExpr(tok, n);
        lit.Type = Type.Int;  // resolve here
        return lit;
      } else {
        return CreateIntSub(tok, CreateIntLiteral(tok, 0), CreateIntLiteral(tok, -n));
      }
    }
    
    static Expression CreateIntSub(IToken tok, Expression e0, Expression e1)
     {
      Contract.Requires(tok != null);
      Contract.Requires(e0 != null);
      Contract.Requires(e1 != null);
      

     Contract.Requires( e0.Type is IntType && e1.Type is IntType);
      Contract.Ensures(Contract.Result<Expression>() != null);
      BinaryExpr s = new BinaryExpr(tok, BinaryExpr.Opcode.Sub, e0, e1);
      s.ResolvedOp = BinaryExpr.ResolvedOpcode.Sub;  // resolve here
      s.Type = Type.Int;  // resolve here
      return s;
    }
    
    static Expression CreateIntITE(IToken tok, Expression test, Expression e0, Expression e1)
     {
      Contract.Requires(tok != null);
      Contract.Requires(test != null);
      Contract.Requires(e0 != null);
      Contract.Requires(e1 != null);
     Contract.Requires( test.Type is BoolType && e0.Type is IntType && e1.Type is IntType);
      Contract.Ensures(Contract.Result<Expression>() != null);

      ITEExpr ite = new ITEExpr(tok, test, e0, e1);
      ite.Type = Type.Int;  // resolve here
      return ite;
    }
    
    public IEnumerable<Expression> Conjuncts(Expression expr)
     {
      Contract.Requires(expr != null);
     Contract.Requires( expr.Type is BoolType);
      Contract.Ensures(cce.NonNullElements(Contract.Result<IEnumerable<Expression>>()));

      if (expr is BinaryExpr) {
        BinaryExpr bin = (BinaryExpr)expr;
        if (bin.ResolvedOp == BinaryExpr.ResolvedOpcode.And) {
          foreach (Expression e in Conjuncts(bin.E0)) {
            yield return e;
          }
          Contract.Assert( bin != null);  // the necessity of this cast is a compiler bug, but perhaps an irrepairable one
          foreach (Expression e in Conjuncts(bin.E1)) {
            yield return e;
          }
          yield break;
        }
      }
      yield return expr;
    }

    List<Bpl.Expr> RecordDecreasesValue(List<Expression> decreases, Bpl.StmtListBuilder builder, Bpl.VariableSeq locals, ExpressionTranslator etran, string varPrefix)
    {
      Contract.Requires(locals != null);
      Contract.Requires(etran != null);
      Contract.Requires(varPrefix != null);
    Contract.Requires(builder != null);
    Contract.Requires(decreases != null);
      List<Bpl.Expr> oldBfs = new List<Bpl.Expr>();
      int c = 0;
      foreach (Expression e in decreases) {
    Contract.Assert(e != null);
        Bpl.LocalVariable bfVar = new Bpl.LocalVariable(e.tok, new Bpl.TypedIdent(e.tok, varPrefix + c, TrType(cce.NonNull(e.Type))));
        locals.Add(bfVar);
        Bpl.IdentifierExpr bf = new Bpl.IdentifierExpr(e.tok, bfVar);
        oldBfs.Add(bf);
        // record value of each decreases expression at beginning of the loop iteration
        Bpl.Cmd cmd = Bpl.Cmd.SimpleAssign(e.tok, bf, etran.TrExpr(e));
        builder.Add(cmd);
        
        c++;
      }
      return oldBfs;
    }
        
    void CheckCallTermination(IToken/*!*/ tok, List<Expression/*!*/>/*!*/ contextDecreases, List<Expression/*!*/>/*!*/ calleeDecreases,
                              Expression receiverReplacement, Dictionary<IVariable,Expression/*!*/>/*!*/ substMap,
                              ExpressionTranslator/*!*/ etran, Bpl.StmtListBuilder/*!*/ builder){
      Contract.Requires(tok != null);
      Contract.Requires(cce.NonNullElements(contextDecreases));
      Contract.Requires(cce.NonNullElements(calleeDecreases));
      Contract.Requires(cce.NonNullElements(substMap));
      Contract.Requires(etran != null);
      Contract.Requires(builder != null);

      int N = Math.Min(contextDecreases.Count, calleeDecreases.Count);
      List<IToken> toks = new List<IToken>();
      List<Type> types = new List<Type>();
      List<Bpl.Expr> callee = new List<Bpl.Expr>();
      List<Bpl.Expr> caller = new List<Bpl.Expr>();
      for (int i = 0; i < N; i++) {
        Expression e0 = Substitute(calleeDecreases[i], receiverReplacement, substMap);
        Expression e1 = contextDecreases[i];
        if (!CompatibleDecreasesTypes(cce.NonNull(e0.Type), cce.NonNull(e1.Type))) {
          break;
        }
        toks.Add(tok);
        types.Add(e0.Type);
        callee.Add(etran.TrExpr(e0));
        caller.Add(etran.TrExpr(e1));
      }
      Bpl.Expr decrExpr = DecreasesCheck(toks, types, callee, caller, etran, builder, "", false);
      builder.Add(Assert(tok, decrExpr, "failure to decrease termination measure"));
    }
    
    /// <summary>
    /// Returns the expression that says whether or not the decreases function has gone down (if !allowNoChange)
    /// or has gone down or stayed the same (if allowNoChange).
    /// ee0 represents the new values and ee1 represents old values.
    /// If builder is non-null, then the check '0 ATMOST decr' is generated to builder.
    /// </summary>
    Bpl.Expr DecreasesCheck(List<IToken/*!*/>/*!*/ toks, List<Type/*!*/>/*!*/ types, List<Bpl.Expr/*!*/>/*!*/ ee0, List<Bpl.Expr/*!*/>/*!*/ ee1,
                             ExpressionTranslator/*!*/ etran, Bpl.StmtListBuilder builder, string suffixMsg, bool allowNoChange)
     {
      Contract.Requires(cce.NonNullElements(toks));
      Contract.Requires(cce.NonNullElements(types));
      Contract.Requires(cce.NonNullElements(ee0));
      Contract.Requires(cce.NonNullElements(ee1));
      Contract.Requires(etran != null);
      Contract.Requires( predef != null);
      Contract.Requires( types.Count == ee0.Count && ee0.Count == ee1.Count);
     Contract.Requires( builder != null && suffixMsg != null);
    Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

      int N = types.Count;
      
      // compute eq and less for each component of the lexicographic pair
      List<Bpl.Expr> Eq = new List<Bpl.Expr>(N);
      List<Bpl.Expr> Less = new List<Bpl.Expr>(N);
      for (int i = 0; i < N; i++) {
        Bpl.Expr less, atmost, eq;
        ComputeLessEq(toks[i], types[i], ee0[i], ee1[i], out less, out atmost, out eq, etran);
        Eq.Add(eq);
        Less.Add(allowNoChange ? atmost : less);
      }
      if (builder != null) {
        // check: 0 <= ee1
        // more precisely, for component k of the lexicographic decreases function, check:
        //   ee0[0] < ee1[0] || ee0[1] < ee1[1] || ... || ee0[k-1] < ee1[k-1] || ee0[k] == ee1[k] || 0 <= ee1[k]
        for (int k = 0; k < N; k++) {
          // we only need to check lower bound for integers--sets, sequences, booleans, references, and datatypes all have natural lower bounds
          Bpl.Expr prefixIsLess = Bpl.Expr.False;
          for (int i = 0; i < k; i++) {
            prefixIsLess = Bpl.Expr.Or(prefixIsLess, Less[i]);
          }
          if (types[k] is IntType) {
            Bpl.Expr bounded = Bpl.Expr.Le(Bpl.Expr.Literal(0), ee1[k]);
            for (int i = 0; i < k; i++) {
              bounded = Bpl.Expr.Or(bounded, Less[i]);
            }
            string component = N == 1 ? "" : " (component " + k + ")";
            Bpl.Cmd cmd = Assert(toks[k], Bpl.Expr.Or(bounded, Eq[k]), "decreases expression" + component + " must be bounded below by 0" + suffixMsg);
            builder.Add(cmd);
          }
        }
      }
      // check: ee0 < ee1 (or ee0 <= ee1, if allowNoChange)
      Bpl.Expr decrCheck = allowNoChange ? Bpl.Expr.True : Bpl.Expr.False;
      for (int i = N; 0 <= --i; ) {
        Bpl.Expr less = Less[i];
        Bpl.Expr eq = Eq[i];
        if (allowNoChange) {
          // decrCheck = atmost && (eq ==> decrCheck)
          decrCheck = Bpl.Expr.And(less, Bpl.Expr.Imp(eq, decrCheck));
        } else {
          // decrCheck = less || (eq && decrCheck)
          decrCheck = Bpl.Expr.Or(less, Bpl.Expr.And(eq, decrCheck));
        }
      }
      return decrCheck;
    }

    bool CompatibleDecreasesTypes(Type t, Type u) {
      Contract.Requires(t != null);
      Contract.Requires(u != null);
      if (t is BoolType) {
        return u is BoolType;
      } else if (t is IntType) {
        return u is IntType;
      } else if (t is SetType) {
        return u is SetType;
      } else if (t is SeqType) {
        return u is SeqType;
      } else if (t.IsDatatype) {
        return u.IsDatatype;
      } else {
        Contract.Assert( t.IsRefType);
        return u.IsRefType;
      }
    }
    
    void ComputeLessEq(IToken/*!*/ tok, Type/*!*/ ty, Bpl.Expr/*!*/ e0, Bpl.Expr/*!*/ e1, out Bpl.Expr/*!*/ less, out Bpl.Expr/*!*/ atmost, out Bpl.Expr/*!*/ eq, ExpressionTranslator/*!*/ etran)
     {
      Contract.Requires(tok != null);
      Contract.Requires(ty != null);
      Contract.Requires(e0 != null);
      Contract.Requires(e1 != null);
      Contract.Requires(etran != null);
      Contract.Requires( predef != null);
      Contract.Ensures(Contract.ValueAtReturn(out less)!=null);
      Contract.Ensures(Contract.ValueAtReturn(out atmost)!=null);
      Contract.Ensures(Contract.ValueAtReturn(out eq)!=null);
     
      if (ty is BoolType) {
        eq = Bpl.Expr.Iff(e0, e1);
        less = Bpl.Expr.And(Bpl.Expr.Not(e0), e1);
        atmost = Bpl.Expr.Imp(e0, e1);
      } else if (ty is IntType) {
        eq = Bpl.Expr.Eq(e0, e1);
        less = Bpl.Expr.Lt(e0, e1);
        atmost = Bpl.Expr.Le(e0, e1);
      } else if (ty is SetType) {
        eq = FunctionCall(tok, BuiltinFunction.SetEqual, null, e0, e1);
        less = etran.ProperSubset(tok, e0, e1);
        atmost = FunctionCall(tok, BuiltinFunction.SetSubset, null, e0, e1);
      } else if (ty is SeqType) {
        Bpl.Expr b0 = FunctionCall(tok, BuiltinFunction.SeqLength, null, e0);
        Bpl.Expr b1 = FunctionCall(tok, BuiltinFunction.SeqLength, null, e1);
        eq = Bpl.Expr.Eq(b0, b1);
        less = Bpl.Expr.Lt(b0, b1);
        atmost = Bpl.Expr.Le(b0, b1);
      } else if (ty.IsDatatype) {
        Bpl.Expr b0 = FunctionCall(tok, BuiltinFunction.DtRank, null, e0);
        Bpl.Expr b1 = FunctionCall(tok, BuiltinFunction.DtRank, null, e1);
        eq = Bpl.Expr.Eq(b0, b1);
        less = Bpl.Expr.Lt(b0, b1);
        atmost = Bpl.Expr.Le(b0, b1);
        
      } else {
        // reference type
        Bpl.Expr b0 = Bpl.Expr.Neq(e0, predef.Null);
        Bpl.Expr b1 = Bpl.Expr.Neq(e1, predef.Null);
        eq = Bpl.Expr.Iff(b0, b1);
        less = Bpl.Expr.And(Bpl.Expr.Not(b0), b1);
        atmost = Bpl.Expr.Imp(b0, b1);
      }
    }
    
    void AddComment(Bpl.StmtListBuilder builder, Statement stmt, string comment) {
      Contract.Requires(builder != null);
      Contract.Requires(stmt != null);
      Contract.Requires(comment != null);
      builder.Add(new Bpl.CommentCmd(string.Format("----- {0} ----- {1}({2},{3})", comment, stmt.Tok.filename, stmt.Tok.line, stmt.Tok.col)));
    }    
    
    Bpl.Expr GetWhereClause(IToken tok, Bpl.Expr x, Type type, ExpressionTranslator etran)
     {
      Contract.Requires(tok != null);
      Contract.Requires(x != null);
      Contract.Requires(type != null);
      Contract.Requires(etran != null);
     Contract.Requires( predef != null);
      if (type is TypeProxy) {
        // unresolved proxy
        Contract.Assert( ((TypeProxy)type).T == null);
        // omit where clause (in other places, unresolved proxies are treated as a reference type; we could do that here too, but
        // we might as well leave out the where clause altogether)
        return null;
      } else if (type is BoolType || type is IntType) {
        return null;
      } else if (type is SetType) {
        SetType st = (SetType)type;
        if (st.Arg.IsRefType) {
          // (forall t: BoxType :: { x[t] } x[t] ==> Unbox(t) == null || $Heap[Unbox(t),alloc])
          Bpl.BoundVariable tVar = new Bpl.BoundVariable(tok, new Bpl.TypedIdent(tok, "$t", predef.BoxType));
          Bpl.Expr t = new Bpl.IdentifierExpr(tok, tVar);
          Bpl.Expr xSubT = Bpl.Expr.SelectTok(tok, x, t);
          Bpl.Expr unboxT = FunctionCall(tok, BuiltinFunction.Unbox, predef.RefType, t);

          Bpl.Trigger tr = new Bpl.Trigger(tok, true, new Bpl.ExprSeq(xSubT));
          
          Bpl.Expr goodRef = etran.GoodRef(tok, unboxT, st.Arg);
          Bpl.Expr body = Bpl.Expr.Imp(xSubT, Bpl.Expr.Or(Bpl.Expr.Eq(unboxT, predef.Null), goodRef));
          return new Bpl.ForallExpr(tok, new Bpl.VariableSeq(tVar), tr, body);
        } else {
          // TODO:  should also handle sets of sets, etc.
          return null;
        }
        
      } else if (type is SeqType) {
        SeqType st = (SeqType)type;
        if (st.Arg.IsRefType) {
          // (forall i: int :: { Seq#Index(x,i) }
          //      0 <= i && i < Seq#Length(x) ==> Unbox(Seq#Index(x,i)) == null || $Heap[Unbox(Seq#Index(x,i)), alloc])
          Bpl.BoundVariable iVar = new Bpl.BoundVariable(tok, new Bpl.TypedIdent(tok, "$i", Bpl.Type.Int));
          Bpl.Expr i = new Bpl.IdentifierExpr(tok, iVar);
          Bpl.Expr xSubI = FunctionCall(tok, BuiltinFunction.SeqIndex, predef.BoxType, x, i);
          Bpl.Expr unbox = FunctionCall(tok, BuiltinFunction.Unbox, predef.RefType, xSubI);

          Bpl.Expr range = InSeqRange(tok, i, x, true, null, false);

          Bpl.Trigger tr = new Bpl.Trigger(tok, true, new Bpl.ExprSeq(xSubI));

          Bpl.Expr goodRef = etran.GoodRef(tok, unbox, st.Arg);
          Bpl.Expr body = Bpl.Expr.Imp(range, Bpl.Expr.Or(Bpl.Expr.Eq(unbox, predef.Null), goodRef));
          return new Bpl.ForallExpr(tok, new Bpl.VariableSeq(iVar), tr, body);
        } else {
          // TODO: should also handle sequences or sequences, etc.
          return null;
        }

      } else if (type.IsRefType) {
        // reference type:
        // x == null || $Heap[x,alloc]
        return Bpl.Expr.Or(Bpl.Expr.Eq(x, predef.Null), etran.GoodRef(tok, x, type));
      } else {
        // type parameter
        return null;
      }
    }
    
    void TrAssignment(IToken tok, Expression lhs, AssignmentRhs rhs, Bpl.StmtListBuilder builder, Bpl.VariableSeq locals,
                      ExpressionTranslator etran)
    {
      Contract.Requires(tok != null);
      Contract.Requires(lhs != null);
      Contract.Requires(rhs != null);
      Contract.Requires(builder != null);
      Contract.Requires(cce.NonNullElements(locals));
      Contract.Requires(etran != null);
      Contract.Requires( predef != null);
      if (rhs is ExprRhs) {
        builder.Add(Assert(tok, IsTotal(lhs, etran), "LHS expression must be well defined"));  // totality check
        builder.Add(Assert(tok, IsTotal(((ExprRhs)rhs).Expr, etran), "RHS expression must be well defined"));  // totality check
        Bpl.Expr bRhs = etran.TrExpr(((ExprRhs)rhs).Expr);
        if (lhs is IdentifierExpr) {
          Bpl.IdentifierExpr bLhs = (Bpl.IdentifierExpr)etran.TrExpr(lhs);  // TODO: is this cast always justified?
          Bpl.Cmd cmd = Bpl.Cmd.SimpleAssign(tok, bLhs, bRhs);
          builder.Add(cmd);
        } else if (lhs is FieldSelectExpr) {
          FieldSelectExpr fse = (FieldSelectExpr)lhs;
          Contract.Assert( fse.Field != null);
          // check that the enclosing modifies clause allows this object to be written:  Contract.Assert( $_Frame[obj]);
          builder.Add(Assert(tok, Bpl.Expr.SelectTok(tok, etran.TheFrame(tok), etran.TrExpr(fse.Obj), GetField(fse)), "assignment may update an object not in the enclosing method's modifies clause"));
          
          Bpl.IdentifierExpr h = cce.NonNull((Bpl.IdentifierExpr)etran.HeapExpr);  // TODO: is this cast always justified?
          bRhs = etran.CondApplyBox(tok, bRhs, cce.NonNull((ExprRhs)rhs).Expr.Type, fse.Field.Type);
          Bpl.Cmd cmd = Bpl.Cmd.MapAssign(tok, h, etran.TrExpr(fse.Obj), new Bpl.IdentifierExpr(tok, GetField(fse.Field)), bRhs);
          builder.Add(cmd);
          // assume $IsGoodHeap($Heap);
          builder.Add(AssumeGoodHeap(tok, etran));
        } else {
          SeqSelectExpr sel = (SeqSelectExpr)lhs;
          Contract.Assert( sel.Seq.Type != null && sel.Seq.Type.IsArrayType);
          bRhs = etran.BoxIfNecessary(tok, bRhs, UserDefinedType.ArrayElementType(sel.Seq.Type));
          if (sel.SelectOne) {
            Contract.Assert( sel.E0 != null);
            Bpl.Expr fieldName = FunctionCall(tok, BuiltinFunction.IndexField, null, etran.TrExpr(sel.E0));
            // check that the enclosing modifies clause allows this object to be written:  Contract.Assert( $_Frame[obj,index]);
            builder.Add(Assert(tok, Bpl.Expr.SelectTok(tok, etran.TheFrame(tok), etran.TrExpr(sel.Seq), fieldName), "assignment may update an array not in the enclosing method's modifies clause"));
            
            Bpl.IdentifierExpr h = cce.NonNull((Bpl.IdentifierExpr)etran.HeapExpr);  // TODO: is this cast always justified?
            Bpl.Cmd cmd = Bpl.Cmd.MapAssign(tok, h, etran.TrExpr(sel.Seq), fieldName, bRhs);
            builder.Add(cmd);
            // assume $IsGoodHeap($Heap);
            builder.Add(AssumeGoodHeap(tok, etran));
          } else {
            Bpl.Expr low = sel.E0 == null ? Bpl.Expr.Literal(0) : etran.TrExpr(sel.E0);
            Bpl.Expr high = sel.E1 == null ? FunctionCall(tok, BuiltinFunction.ArrayLength, null, etran.TrExpr(sel.Seq)) : etran.TrExpr(sel.E1);
            // check frame:
            // assert (forall i: int :: low <= i && i < high ==> $_Frame[arr,i]);
            Bpl.Variable iVar = new Bpl.BoundVariable(tok, new Bpl.TypedIdent(tok, "$i", Bpl.Type.Int));
            Bpl.IdentifierExpr ie = new Bpl.IdentifierExpr(tok, iVar);
            Bpl.Expr ante = Bpl.Expr.And(Bpl.Expr.Le(low, ie), Bpl.Expr.Lt(ie, high));
            Bpl.Expr fieldName = FunctionCall(tok, BuiltinFunction.IndexField, null, ie);
            Bpl.Expr cons = Bpl.Expr.SelectTok(tok, etran.TheFrame(tok), etran.TrExpr(sel.Seq), fieldName);
            Bpl.Expr q = new Bpl.ForallExpr(tok, new Bpl.VariableSeq(iVar), Bpl.Expr.Imp(ante, cons));
            builder.Add(Assert(tok, q, "assignment may update an array not in the enclosing method's modifies clause"));
            // do the update:  call UpdateArrayRange(arr, low, high, rhs);
            builder.Add(new Bpl.CallCmd(tok, "UpdateArrayRange",
              new Bpl.ExprSeq(etran.TrExpr(sel.Seq), low, high, bRhs),
              new Bpl.IdentifierExprSeq()));
          }
        }
        
      } else if (rhs is HavocRhs) {
        Contract.Assert( lhs is IdentifierExpr);  // for this kind of RHS, the LHS is restricted to be a simple variable
        Bpl.IdentifierExpr x = (Bpl.IdentifierExpr)etran.TrExpr(lhs);  // TODO: is this cast always justified?
        builder.Add(new Bpl.HavocCmd(tok, new Bpl.IdentifierExprSeq(x)));

      } else {
        Contract.Assert( rhs is TypeRhs);  // otherwise, an unexpected AssignmentRhs
        TypeRhs tRhs = (TypeRhs)rhs;
        Contract.Assert( lhs is IdentifierExpr);  // for this kind of RHS, the LHS is restricted to be a simple variable
        
        if (tRhs.ArraySize != null) {
          CheckWellformed(tRhs.ArraySize, null, Position.Positive, locals, builder, etran);
          builder.Add(Assert(tok, Bpl.Expr.Le(Bpl.Expr.Literal(0), etran.TrExpr(tRhs.ArraySize)), "array size might be negative"));
        }
        
        Bpl.IdentifierExpr nw = GetNewVar_IdExpr(tok, locals);
        builder.Add(new Bpl.HavocCmd(tok, new Bpl.IdentifierExprSeq(nw)));
        // assume $nw != null && !$Heap[$nw, alloc] && dtype($nw) == RHS;
        Bpl.Expr nwNotNull = Bpl.Expr.Neq(nw, predef.Null);
        Bpl.Expr rightType;
        if (tRhs.ArraySize != null) {
          // array allocation
          List<Type> typeArgs = new List<Type>();
          typeArgs.Add(tRhs.EType);
          rightType = etran.GoodRef_Ref(tok, nw, new Bpl.IdentifierExpr(tok, "class.array", predef.ClassNameType), typeArgs, true);
        } else if (tRhs.EType is ObjectType) {
          rightType = etran.GoodRef_Ref(tok, nw, new Bpl.IdentifierExpr(tok, "class.object", predef.ClassNameType), new List<Type>(), true);
        } else {
          rightType = etran.GoodRef_Class(tok, nw, (UserDefinedType)tRhs.EType, true);
        }
        builder.Add(new Bpl.AssumeCmd(tok, Bpl.Expr.And(nwNotNull, rightType)));
        if (tRhs.ArraySize != null) {
          // assume Array#Length($nw) == arraySize;
          Bpl.Expr arrayLength = FunctionCall(tok, BuiltinFunction.ArrayLength, null, nw);
          builder.Add(new Bpl.AssumeCmd(tok, Bpl.Expr.Eq(arrayLength, etran.TrExpr(tRhs.ArraySize))));
        }
        // $Heap[$nw, alloc] := true;
        Bpl.Expr alloc = predef.Alloc(tok);
        Bpl.Cmd cmd = Bpl.Cmd.MapAssign(tok, (Bpl.IdentifierExpr/*TODO: this cast is dubious*/)etran.HeapExpr, nw, alloc, Bpl.Expr.True);
        builder.Add(cmd);
        // x := $nw;
        Bpl.IdentifierExpr x = (Bpl.IdentifierExpr)etran.TrExpr(lhs);  // TODO: is this cast always justified?
        builder.Add(Bpl.Cmd.SimpleAssign(tok, x, nw));
        // assume $IsGoodHeap($Heap);
        builder.Add(AssumeGoodHeap(tok, etran));
      }
    }
    
    Bpl.AssumeCmd AssumeGoodHeap(IToken tok, ExpressionTranslator etran) {
      Contract.Requires(tok != null);
      Contract.Requires(etran != null);
      Contract.Ensures(Contract.Result<AssumeCmd>() != null);

      return new Bpl.AssumeCmd(tok, FunctionCall(tok, BuiltinFunction.IsGoodHeap, null, etran.HeapExpr));
    }
    
    // ----- Expression ---------------------------------------------------------------------------
    
    internal class ExpressionTranslator {
      public readonly Bpl.Expr HeapExpr;
      public readonly PredefinedDecls predef;
      public readonly Translator translator;
      public readonly string This;
      readonly Function applyLimited_CurrentFunction;
      [ContractInvariantMethod]
void ObjectInvariant() 
{
    Contract.Invariant(HeapExpr!=null);
        Contract.Invariant(predef != null);
        Contract.Invariant(translator != null);
        Contract.Invariant(This != null);
}

      
      public ExpressionTranslator(Translator translator, PredefinedDecls predef, IToken heapToken) {
        Contract.Requires(translator != null);
        Contract.Requires(predef != null);
        Contract.Requires(heapToken != null);
        this.translator = translator;
        this.predef = predef;
        this.HeapExpr = new Bpl.IdentifierExpr(heapToken, predef.HeapVarName, predef.HeapType);
        this.This = "this";
      }
      
      public ExpressionTranslator(Translator translator, PredefinedDecls predef, Bpl.Expr heap) {
        Contract.Requires(translator != null);
        Contract.Requires(predef != null);
        Contract.Requires(heap != null);
        this.translator = translator;
        this.predef = predef;
        this.HeapExpr = heap;
        this.This = "this";
      }
      
      public ExpressionTranslator(Translator translator, PredefinedDecls predef, Bpl.Expr heap, string thisVar) {
        Contract.Requires(translator != null);
        Contract.Requires(predef != null);
        Contract.Requires(heap != null);
        Contract.Requires(thisVar != null);
        this.translator = translator;
        this.predef = predef;
        this.HeapExpr = heap;
        this.This = thisVar;
      }
      
      ExpressionTranslator(Translator translator, PredefinedDecls predef, Bpl.Expr heap, Function applyLimited_CurrentFunction) {
        Contract.Requires(translator != null);
        Contract.Requires(predef != null);
        Contract.Requires(heap != null);
        Contract.Requires(applyLimited_CurrentFunction != null);
        this.translator = translator;
        this.predef = predef;
        this.HeapExpr = heap;
        this.applyLimited_CurrentFunction = applyLimited_CurrentFunction;
        this.This = "this";
      }     

      ExpressionTranslator oldEtran;
      public ExpressionTranslator Old {
        get {
          Contract.Ensures(Contract.Result<ExpressionTranslator>() != null);

          if (oldEtran == null) {
            oldEtran = new ExpressionTranslator(translator, predef, new Bpl.OldExpr(HeapExpr.tok, HeapExpr), applyLimited_CurrentFunction);
          }
          return oldEtran;
        }
      }
      
      public ExpressionTranslator LimitedFunctions(Function applyLimited_CurrentFunction) {
        Contract.Requires(applyLimited_CurrentFunction != null);
        Contract.Ensures(Contract.Result<ExpressionTranslator>() != null);

        return new ExpressionTranslator(translator, predef, HeapExpr, applyLimited_CurrentFunction);
      }
      
      public Bpl.IdentifierExpr TheFrame(IToken tok)
      {
        Contract.Requires(tok != null);
      Contract.Ensures(Contract.Result<Bpl.IdentifierExpr>() != null);
        Contract.Ensures(  Contract.Result<Bpl.IdentifierExpr>().Type != null);

        Bpl.TypeVariable alpha = new Bpl.TypeVariable(tok, "beta");
        Bpl.Type fieldAlpha = predef.FieldName(tok, alpha);
        Bpl.Type ty = new Bpl.MapType(tok, new Bpl.TypeVariableSeq(alpha), new Bpl.TypeSeq(predef.RefType, fieldAlpha), Bpl.Type.Bool);
        return new Bpl.IdentifierExpr(tok, "$_Frame", ty);
      }
      
      public Bpl.IdentifierExpr ModuleContextHeight()
        {
       Contract.Ensures(  Contract.Result<Bpl.IdentifierExpr>().Type != null);
        return new Bpl.IdentifierExpr(Token.NoToken, "$ModuleContextHeight", Bpl.Type.Int);
      }

      public Bpl.IdentifierExpr FunctionContextHeight()
        {
       Contract.Ensures( Contract.Result<Bpl.IdentifierExpr>().Type != null);
        return new Bpl.IdentifierExpr(Token.NoToken, "$FunctionContextHeight", Bpl.Type.Int);
      }

      public Bpl.IdentifierExpr InMethodContext()
        {
       Contract.Ensures(  Contract.Result<Bpl.IdentifierExpr>().Type != null);
        return new Bpl.IdentifierExpr(Token.NoToken, "$InMethodContext", Bpl.Type.Bool);
      }

      public Bpl.Expr TrExpr(Expression expr)
       {
        Contract.Requires(expr != null);
       Contract.Requires( predef != null);
        if (expr is LiteralExpr) {
          LiteralExpr e = (LiteralExpr)expr;
          if (e.Value == null) {
            return predef.Null;
          } else if (e.Value is bool) {
            return Bpl.Expr.Literal((bool)e.Value);
          } else if (e.Value is BigInteger) {
            return Bpl.Expr.Literal(Microsoft.Basetypes.BigNum.FromBigInt((BigInteger)e.Value));
          } else {
            Contract.Assert(false); throw new cce.UnreachableException();  // unexpected literal
          }
          
        } else if (expr is ThisExpr) {
          return new Bpl.IdentifierExpr(expr.tok, This, predef.RefType);
          
        } else if (expr is IdentifierExpr) {
          IdentifierExpr e = (IdentifierExpr)expr;
          return TrVar(expr.tok, cce.NonNull(e.Var));
        
        } else if (expr is SetDisplayExpr) {
          SetDisplayExpr e = (SetDisplayExpr)expr;
          Bpl.Expr s = translator.FunctionCall(expr.tok, BuiltinFunction.SetEmpty, predef.BoxType);
          foreach (Expression ee in e.Elements) {
            Bpl.Expr ss = BoxIfNecessary(expr.tok, TrExpr(ee), cce.NonNull(ee.Type));
            s = translator.FunctionCall(expr.tok, BuiltinFunction.SetUnionOne, predef.BoxType, s, ss);
          }
          return s;
          
        } else if (expr is SeqDisplayExpr) {
          SeqDisplayExpr e = (SeqDisplayExpr)expr;
          Bpl.Expr s = translator.FunctionCall(expr.tok, BuiltinFunction.SeqEmpty, predef.BoxType);
          int i = 0;
          foreach (Expression ee in e.Elements) {
            Bpl.Expr ss = BoxIfNecessary(expr.tok, TrExpr(ee), cce.NonNull(ee.Type));
            s = translator.FunctionCall(expr.tok, BuiltinFunction.SeqBuild, predef.BoxType, s, Bpl.Expr.Literal(i), ss, Bpl.Expr.Literal(i+1));
            i++;
          }
          return s;
        
        } else if (expr is FieldSelectExpr) {
          FieldSelectExpr e = (FieldSelectExpr)expr;
          Bpl.Expr result = Bpl.Expr.SelectTok(expr.tok, HeapExpr, TrExpr(e.Obj), new Bpl.IdentifierExpr(expr.tok, translator.GetField(cce.NonNull(e.Field))));
          return CondApplyUnbox(expr.tok, result, e.Field.Type, cce.NonNull(expr.Type));
          
        } else if (expr is SeqSelectExpr) {
          SeqSelectExpr e = (SeqSelectExpr)expr;
          Bpl.Expr seq = TrExpr(e.Seq);
          Type elmtType;
          Contract.Assert( e.Seq.Type != null);  // the expression has been successfully resolved
          if (e.Seq.Type.IsArrayType) {
            Contract.Assert( e.SelectOne);  // resolution enforces that a non-unit array selections is allowed only as an assignment LHS
            elmtType = UserDefinedType.ArrayElementType(e.Seq.Type);
          } else {
            elmtType = ((SeqType)e.Seq.Type).Arg;
          }
          Bpl.Type elType = translator.TrType(elmtType);
          Bpl.Expr e0 = e.E0 == null ? null : TrExpr(e.E0);
          Bpl.Expr e1 = e.E1 == null ? null : TrExpr(e.E1);
          if (e.SelectOne) {
            Contract.Assert( e1 == null);
            Bpl.Expr x;
            if (e.Seq.Type.IsArrayType) {
              Bpl.Expr fieldName = translator.FunctionCall(expr.tok, BuiltinFunction.IndexField, null, e0);
              x = Bpl.Expr.SelectTok(expr.tok, HeapExpr, TrExpr(e.Seq), fieldName);
            } else {
              x = translator.FunctionCall(expr.tok, BuiltinFunction.SeqIndex, predef.BoxType, seq, e0);
            }
            if (!ModeledAsBoxType(elmtType)) {
              x = translator.FunctionCall(expr.tok, BuiltinFunction.Unbox, elType, x);
            }
            return x;
          } else {
            if (e1 != null) {
              seq = translator.FunctionCall(expr.tok, BuiltinFunction.SeqTake, elType, seq, e1);
            }
            if (e0 != null) {
              seq = translator.FunctionCall(expr.tok, BuiltinFunction.SeqDrop, elType, seq, e0);
            }
            return seq;
          }
        
        } else if (expr is SeqUpdateExpr) {
          SeqUpdateExpr e = (SeqUpdateExpr)expr;
          Bpl.Expr seq = TrExpr(e.Seq);
          Type elmtType = cce.NonNull((SeqType)e.Seq.Type).Arg;
          Bpl.Expr index = TrExpr(e.Index);
          Bpl.Expr val = BoxIfNecessary(expr.tok, TrExpr(e.Value), elmtType);
          return translator.FunctionCall(expr.tok, BuiltinFunction.SeqUpdate, predef.BoxType, seq, index, val);
        
        } else if (expr is FunctionCallExpr) {
          FunctionCallExpr e = (FunctionCallExpr)expr;
          string nm = cce.NonNull(e.Function).FullName;
          if (this.applyLimited_CurrentFunction != null && e.Function.IsRecursive && !e.Function.IsUnlimited) {
            ModuleDecl module = cce.NonNull(e.Function.EnclosingClass).Module;
            if (module == cce.NonNull(applyLimited_CurrentFunction.EnclosingClass).Module) {
              if (module.CallGraph.GetSCCRepresentative(e.Function) == module.CallGraph.GetSCCRepresentative(applyLimited_CurrentFunction)) {
                nm += "#limited";
              }
            }
          }
          Bpl.IdentifierExpr id = new Bpl.IdentifierExpr(expr.tok, nm, translator.TrType(cce.NonNull(e.Type)));
          Bpl.ExprSeq args = new Bpl.ExprSeq();
          args.Add(HeapExpr);
          if (!e.Function.IsStatic) {
            args.Add(TrExpr(e.Receiver));
          }
          for (int i = 0; i < e.Args.Count; i++) {
            Expression ee = e.Args[i];
            Type t = e.Function.Formals[i].Type;
            args.Add(CondApplyBox(expr.tok, TrExpr(ee), cce.NonNull(ee.Type), t));
          }
          Bpl.Expr result = new Bpl.NAryExpr(expr.tok, new Bpl.FunctionCall(id), args);
          return CondApplyUnbox(expr.tok, result, e.Function.ResultType, expr.Type);
        
        } else if (expr is DatatypeValue) {
          DatatypeValue dtv = (DatatypeValue)expr;
          Contract.Assert( dtv.Ctor != null);  // since dtv has been successfully resolved
          Bpl.ExprSeq args = new Bpl.ExprSeq();
          for (int i = 0; i < dtv.Arguments.Count; i++) {
            Expression arg = dtv.Arguments[i];
            Type t = dtv.Ctor.Formals[i].Type;
            args.Add(CondApplyBox(expr.tok, TrExpr(arg), cce.NonNull(arg.Type), t));
          }
          Bpl.IdentifierExpr id = new Bpl.IdentifierExpr(dtv.tok, dtv.Ctor.FullName, predef.DatatypeType);
          return new Bpl.NAryExpr(dtv.tok, new Bpl.FunctionCall(id), args);
            
        } else if (expr is OldExpr) {
          OldExpr e = (OldExpr)expr;
          return new Bpl.OldExpr(expr.tok, TrExpr(e.E));
        
        } else if (expr is FreshExpr) {
          FreshExpr e = (FreshExpr)expr;
          Bpl.Expr oldHeap = new Bpl.OldExpr(expr.tok, HeapExpr);
          if (e.E.Type is SetType) {
            // generate:  (forall $o: ref :: $o != null && X[Box($o)] ==> !old($Heap)[$o,alloc])
            // TODO: trigger?
            Bpl.Variable oVar = new Bpl.BoundVariable(expr.tok, new Bpl.TypedIdent(expr.tok, "$o", predef.RefType));
            Bpl.Expr o = new Bpl.IdentifierExpr(expr.tok, oVar);
            Bpl.Expr oNotNull = Bpl.Expr.Neq(o, predef.Null);
            Bpl.Expr oInSet = TrInSet(expr.tok, o, e.E, ((SetType)e.E.Type).Arg);
            Bpl.Expr oIsFresh = Bpl.Expr.Not(IsAlloced(expr.tok, o, oldHeap));
            Bpl.Expr body = Bpl.Expr.Imp(Bpl.Expr.And(oNotNull, oInSet), oIsFresh);
            return new Bpl.ForallExpr(expr.tok, new Bpl.VariableSeq(oVar), body);
          } else if (e.E.Type is SeqType) {
            // generate:  (forall $i: int :: 0 <= $i && $i < Seq#Length(X) && Unbox(Seq#Index(X,$i)) != null ==> !old($Heap)[Seq#Index(X,$i),alloc])
            // TODO: trigger?
            Bpl.Variable iVar = new Bpl.BoundVariable(expr.tok, new Bpl.TypedIdent(expr.tok, "$i", Bpl.Type.Int));
            Bpl.Expr i = new Bpl.IdentifierExpr(expr.tok, iVar);
            Bpl.Expr iBounds = translator.InSeqRange(expr.tok, i, TrExpr(e.E), true, null, false);
            Bpl.Expr XsubI = translator.FunctionCall(expr.tok, BuiltinFunction.SeqIndex, predef.RefType, TrExpr(e.E), i);
            Bpl.Expr oIsFresh = Bpl.Expr.Not(IsAlloced(expr.tok, XsubI, oldHeap));
            Bpl.Expr xsubiNotNull = Bpl.Expr.Neq(translator.FunctionCall(expr.tok, BuiltinFunction.Unbox, predef.RefType, XsubI), predef.Null);
            Bpl.Expr body = Bpl.Expr.Imp(Bpl.Expr.And(iBounds, xsubiNotNull), oIsFresh);
            return new Bpl.ForallExpr(expr.tok, new Bpl.VariableSeq(iVar), body);
          } else {
            // generate:  x == null || !old($Heap)[x]
            Bpl.Expr oNull = Bpl.Expr.Eq(TrExpr(e.E), predef.Null);
            Bpl.Expr oIsFresh = Bpl.Expr.Not(IsAlloced(expr.tok, TrExpr(e.E), oldHeap));
            return Bpl.Expr.Or(oNull, oIsFresh);
          }
        
        } else if (expr is UnaryExpr) {
          UnaryExpr e = (UnaryExpr)expr;
          Bpl.Expr arg = TrExpr(e.E);
          switch (e.Op) {
            case UnaryExpr.Opcode.Not:
              return Bpl.Expr.Not(arg);
            case UnaryExpr.Opcode.SeqLength:
              if (e.E.Type is SeqType) {
                return translator.FunctionCall(expr.tok, BuiltinFunction.SeqLength, null, arg);
              } else {
                return translator.FunctionCall(expr.tok, BuiltinFunction.ArrayLength, null, arg);
              }
            default:
              Contract.Assert(false); throw new cce.UnreachableException();  // unexpected unary expression
          }
        
        } else if (expr is BinaryExpr) {
          BinaryExpr e = (BinaryExpr)expr;
          Bpl.Expr e0 = TrExpr(e.E0);
          if (e.ResolvedOp == BinaryExpr.ResolvedOpcode.InSet) {
            return TrInSet(expr.tok, e0, e.E1, cce.NonNull(e.E0.Type));  // let TrInSet translate e.E1
          } else if (e.ResolvedOp == BinaryExpr.ResolvedOpcode.NotInSet) {
            Bpl.Expr arg = TrInSet(expr.tok, e0, e.E1, cce.NonNull(e.E0.Type));  // let TrInSet translate e.E1
            return Bpl.Expr.Not(arg);
          }
          Bpl.Expr e1 = TrExpr(e.E1);
          switch (e.ResolvedOp) {
            case BinaryExpr.ResolvedOpcode.Iff:
              return Bpl.Expr.Iff(e0, e1);
            case BinaryExpr.ResolvedOpcode.Imp:
              return Bpl.Expr.Imp(e0, e1);
            case BinaryExpr.ResolvedOpcode.And:
              return Bpl.Expr.And(e0, e1);
            case BinaryExpr.ResolvedOpcode.Or:
              return Bpl.Expr.Or(e0, e1);
              
            case BinaryExpr.ResolvedOpcode.EqCommon:
              return Bpl.Expr.Eq(e0, e1);
            case BinaryExpr.ResolvedOpcode.NeqCommon:
              return Bpl.Expr.Neq(e0, e1);
              
            case BinaryExpr.ResolvedOpcode.Lt:
              return Bpl.Expr.Lt(e0, e1);
            case BinaryExpr.ResolvedOpcode.Le:
              return Bpl.Expr.Le(e0, e1);
            case BinaryExpr.ResolvedOpcode.Ge:
              return Bpl.Expr.Ge(e0, e1);
            case BinaryExpr.ResolvedOpcode.Gt:
              return Bpl.Expr.Gt(e0, e1);
            case BinaryExpr.ResolvedOpcode.Add:
              return Bpl.Expr.Add(e0, e1);
            case BinaryExpr.ResolvedOpcode.Sub:
              return Bpl.Expr.Sub(e0, e1);
            case BinaryExpr.ResolvedOpcode.Mul:
              return Bpl.Expr.Mul(e0, e1);
            case BinaryExpr.ResolvedOpcode.Div:
              return Bpl.Expr.Div(e0, e1);
            case BinaryExpr.ResolvedOpcode.Mod:
              return Bpl.Expr.Mod(e0, e1);

            case BinaryExpr.ResolvedOpcode.SetEq:
              return translator.FunctionCall(expr.tok, BuiltinFunction.SetEqual, null, e0, e1);
            case BinaryExpr.ResolvedOpcode.SetNeq:
              return Bpl.Expr.Not(translator.FunctionCall(expr.tok, BuiltinFunction.SetEqual, null, e0, e1));
            case BinaryExpr.ResolvedOpcode.ProperSubset:
              return ProperSubset(expr.tok, e0, e1);
            case BinaryExpr.ResolvedOpcode.Subset:
              return translator.FunctionCall(expr.tok, BuiltinFunction.SetSubset, null, e0, e1);
            case BinaryExpr.ResolvedOpcode.Superset:
              return translator.FunctionCall(expr.tok, BuiltinFunction.SetSubset, null, e1, e0);
            case BinaryExpr.ResolvedOpcode.ProperSuperset:
              return Bpl.Expr.And(
                translator.FunctionCall(expr.tok, BuiltinFunction.SetSubset, null, e1, e0),
                Bpl.Expr.Not(translator.FunctionCall(expr.tok, BuiltinFunction.SetEqual, null, e0, e1)));
            case BinaryExpr.ResolvedOpcode.Disjoint:
              return translator.FunctionCall(expr.tok, BuiltinFunction.SetDisjoint, null, e0, e1);
            case BinaryExpr.ResolvedOpcode.InSet:
              Contract.Assert(false); throw new cce.UnreachableException();  // this case handled above
            case BinaryExpr.ResolvedOpcode.Union:
              return translator.FunctionCall(expr.tok, BuiltinFunction.SetUnion, translator.TrType(cce.NonNull((SetType)expr.Type).Arg), e0, e1);
            case BinaryExpr.ResolvedOpcode.Intersection:
              return translator.FunctionCall(expr.tok, BuiltinFunction.SetIntersection, translator.TrType(cce.NonNull((SetType)expr.Type).Arg), e0, e1);
            case BinaryExpr.ResolvedOpcode.SetDifference:
              return translator.FunctionCall(expr.tok, BuiltinFunction.SetDifference, translator.TrType(cce.NonNull((SetType)expr.Type).Arg), e0, e1);

            case BinaryExpr.ResolvedOpcode.SeqEq:
              return translator.FunctionCall(expr.tok, BuiltinFunction.SeqEqual, null, e0, e1);
            case BinaryExpr.ResolvedOpcode.SeqNeq:
              return Bpl.Expr.Not(translator.FunctionCall(expr.tok, BuiltinFunction.SeqEqual, null, e0, e1));
            case BinaryExpr.ResolvedOpcode.ProperPrefix:
              return ProperPrefix(expr.tok, e0, e1);
            case BinaryExpr.ResolvedOpcode.Prefix:
              {
                Bpl.Expr len0 = translator.FunctionCall(expr.tok, BuiltinFunction.SeqLength, null, e0);
                Bpl.Expr len1 = translator.FunctionCall(expr.tok, BuiltinFunction.SeqLength, null, e1);
                return Bpl.Expr.And(
                  Bpl.Expr.Le(len0, len1),
                  translator.FunctionCall(expr.tok, BuiltinFunction.SeqSameUntil, null, e0, e1, len0));
              }
            case BinaryExpr.ResolvedOpcode.Concat:
              return translator.FunctionCall(expr.tok, BuiltinFunction.SeqAppend, translator.TrType(cce.NonNull((SeqType)expr.Type).Arg), e0, e1);
            case BinaryExpr.ResolvedOpcode.InSeq:
              return translator.FunctionCall(expr.tok, BuiltinFunction.SeqContains, null, e1,
                       BoxIfNecessary(expr.tok, e0, cce.NonNull(e.E0.Type)));
            case BinaryExpr.ResolvedOpcode.NotInSeq:
              Bpl.Expr arg = translator.FunctionCall(expr.tok, BuiltinFunction.SeqContains, null, e1,
                       BoxIfNecessary(expr.tok, e0, cce.NonNull(e.E0.Type)));
              return Bpl.Expr.Not(arg);

            case BinaryExpr.ResolvedOpcode.RankLt:
              return Bpl.Expr.Lt(
                translator.FunctionCall(expr.tok, BuiltinFunction.DtRank, null, e0),
                translator.FunctionCall(expr.tok, BuiltinFunction.DtRank, null, e1));
            case BinaryExpr.ResolvedOpcode.RankGt:
              return Bpl.Expr.Gt(
                translator.FunctionCall(expr.tok, BuiltinFunction.DtRank, null, e0),
                translator.FunctionCall(expr.tok, BuiltinFunction.DtRank, null, e1));
              
            default:
              Contract.Assert(false); throw new cce.UnreachableException();  // unexpected binary expression
          }
        
        } else if (expr is QuantifierExpr) {
          QuantifierExpr e = (QuantifierExpr)expr;
          Bpl.VariableSeq bvars = new Bpl.VariableSeq();
          foreach (BoundVar bv in e.BoundVars) {
            bvars.Add(new Bpl.BoundVariable(bv.tok, new Bpl.TypedIdent(bv.tok, bv.UniqueName, translator.TrType(bv.Type))));
          }
          Bpl.QKeyValue kv = TrAttributes(e.Attributes);
          Bpl.Trigger tr = null;
          for (Triggers trigs = e.Trigs; trigs != null; trigs = trigs.Prev) {
            Bpl.ExprSeq tt = new Bpl.ExprSeq();
            foreach (Expression term in trigs.Terms) {
              tt.Add(TrExpr(term));
            }
            tr = new Bpl.Trigger(expr.tok, true, tt, tr);
          }
          Bpl.Expr body = TrExpr(e.Body);
          
          if (e is ForallExpr) {
            return new Bpl.ForallExpr(expr.tok, new Bpl.TypeVariableSeq(), bvars, kv, tr, body);
          } else {
            Contract.Assert( e is ExistsExpr);
            return new Bpl.ExistsExpr(expr.tok, new Bpl.TypeVariableSeq(), bvars, kv, tr, body);
          }
        
        } else if (expr is ITEExpr) {
          ITEExpr e = (ITEExpr)expr;
          Bpl.Expr g = TrExpr(e.Test);
          Bpl.Expr thn = TrExpr(e.Thn);
          Bpl.Expr els = TrExpr(e.Els);
          return translator.FunctionCall(expr.tok, BuiltinFunction.IfThenElse, translator.TrType(cce.NonNull(expr.Type)), g, thn, els);
           
        } else if (expr is BoxingCastExpr) {
          BoxingCastExpr e = (BoxingCastExpr)expr;
          return CondApplyBox(e.tok, TrExpr(e.E), e.FromType, e.ToType);
          
        } else if (expr is UnboxingCastExpr) {
          UnboxingCastExpr e = (UnboxingCastExpr)expr;
          return CondApplyUnbox(e.tok, TrExpr(e.E), e.FromType, e.ToType);
          
        } else {
          Contract.Assert(false); throw new cce.UnreachableException();  // unexpected expression
        }
      }
      
      public Bpl.Expr ProperSubset(IToken tok, Bpl.Expr e0, Bpl.Expr e1) {
        Contract.Requires(tok != null);
        Contract.Requires(e0 != null);
        Contract.Requires(e1 != null);
        Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

        return Bpl.Expr.And(
          translator.FunctionCall(tok, BuiltinFunction.SetSubset, null, e0, e1),
          Bpl.Expr.Not(translator.FunctionCall(tok, BuiltinFunction.SetSubset, null, e1, e0)));
      }
      public Bpl.Expr ProperPrefix(IToken tok, Bpl.Expr e0, Bpl.Expr e1) {
        Contract.Requires(tok != null);
        Contract.Requires(e0 != null);
        Contract.Requires(e1 != null);
        Contract.Ensures(Contract.Result<Bpl.Expr>() != null);
        Bpl.Expr len0 = translator.FunctionCall(tok, BuiltinFunction.SeqLength, null, e0);
        Bpl.Expr len1 = translator.FunctionCall(tok, BuiltinFunction.SeqLength, null, e1);
        return Bpl.Expr.And(
          Bpl.Expr.Lt(len0, len1),
          translator.FunctionCall(tok, BuiltinFunction.SeqSameUntil, null, e0, e1, len0));
      }

      public Bpl.Expr TrUseExpr(FunctionCallExpr e)
       {
        Contract.Requires(e != null); Contract.Requires( e.Function != null && e.Type != null);
        Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

      
        Function fn = e.Function;
        Bpl.ExprSeq args = new Bpl.ExprSeq();
        args.Add(HeapExpr);
        if (!fn.IsStatic) {
          args.Add(TrExpr(e.Receiver));
        }
        foreach (Expression ee in e.Args) {
          args.Add(TrExpr(ee));
        }
        Bpl.Expr f0 = new Bpl.NAryExpr(e.tok, new Bpl.FunctionCall(new Bpl.IdentifierExpr(e.tok, fn.FullName, translator.TrType(e.Type))), args);
        Bpl.Expr f1;
        if (fn.IsRecursive && !fn.IsUnlimited) {
          f1 = new Bpl.NAryExpr(e.tok, new Bpl.FunctionCall(new Bpl.IdentifierExpr(e.tok, fn.FullName + "#limited", translator.TrType(e.Type))), args);
        } else {
          f1 = f0;
        }
        return Bpl.Expr.Eq(f0, f1);
      }

      public Bpl.Expr CondApplyBox(IToken tok, Bpl.Expr e, Type fromType, Type toType) {
        Contract.Requires(tok != null);
        Contract.Requires(e != null);
        Contract.Requires(fromType != null);
        Contract.Requires(toType != null);
        Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

        if (!ModeledAsBoxType(fromType) && ModeledAsBoxType(toType)) {
          return translator.FunctionCall(tok, BuiltinFunction.Box, null, e);
        } else {
          return e;
        }
      }
      
      public Bpl.Expr BoxIfNecessary(IToken tok, Bpl.Expr e, Type fromType) {
        Contract.Requires(tok != null);
        Contract.Requires(e != null);
        Contract.Requires(fromType != null);
        Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

        if (!ModeledAsBoxType(fromType)) {
          return translator.FunctionCall(tok, BuiltinFunction.Box, null, e);
        } else {
          return e;
        }
      }
      
      public Bpl.Expr CondApplyUnbox(IToken tok, Bpl.Expr e, Type fromType, Type toType) {
Contract.Requires(tok != null);
        Contract.Requires(e != null);
        Contract.Requires(fromType != null);
        Contract.Requires(toType != null);
        Contract.Ensures(Contract.Result<Bpl.Expr>() != null);
        if (ModeledAsBoxType(fromType) && !ModeledAsBoxType(toType)) {
          return translator.FunctionCall(tok, BuiltinFunction.Unbox, translator.TrType(toType), e);
        } else {
          return e;
        }
      
      }
      
      public static bool ModeledAsBoxType(Type t) {
        Contract.Requires(t != null);
        while (true) {
          TypeProxy tp = t as TypeProxy;
          if (tp == null) {
            break;
          } else if (tp.T == null) {
            // unresolved proxy
            return false;
          } else {
            t = tp.T;
          }
        }
        return t.IsTypeParameter;
      }

      public Bpl.IdentifierExpr TrVar(IToken tok, IVariable var) {
        Contract.Requires(var != null);
        Contract.Requires(tok != null);
        Contract.Ensures(Contract.Result<Bpl.IdentifierExpr>() != null);

        return new Bpl.IdentifierExpr(tok, var.UniqueName, translator.TrType(var.Type));
      }
      
      /// <summary>
      /// Translate like s[Box(elmt)], but try to avoid as many set functions as possible in the
      /// translation, because such functions can mess up triggering.
      /// </summary>
      public Bpl.Expr TrInSet(IToken tok, Bpl.Expr elmt, Expression s, Type elmtType) {
        Contract.Requires(tok != null);
        Contract.Requires(elmt != null);
        Contract.Requires(s != null);
        Contract.Requires(elmtType != null);

        Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

        if (s is BinaryExpr) {
          BinaryExpr bin = (BinaryExpr)s;
          switch (bin.ResolvedOp) {
            case BinaryExpr.ResolvedOpcode.Union:
              return Bpl.Expr.Or(TrInSet(tok, elmt, bin.E0, elmtType), TrInSet(tok, elmt, bin.E1, elmtType));
            case BinaryExpr.ResolvedOpcode.Intersection:
              return Bpl.Expr.And(TrInSet(tok, elmt, bin.E0, elmtType), TrInSet(tok, elmt, bin.E1, elmtType));
            case BinaryExpr.ResolvedOpcode.SetDifference:
              return Bpl.Expr.And(TrInSet(tok, elmt, bin.E0, elmtType), Bpl.Expr.Not(TrInSet(tok, elmt, bin.E1, elmtType)));
            default:
              break;
          }
        } else if (s is SetDisplayExpr) {
          SetDisplayExpr disp = (SetDisplayExpr)s;
          Bpl.Expr disjunction = null;
          foreach (Expression a in disp.Elements) {
            Bpl.Expr disjunct = Bpl.Expr.Eq(elmt, TrExpr(a));
            if (disjunction == null) {
              disjunction = disjunct;
            } else {
              disjunction = Bpl.Expr.Or(disjunction, disjunct);
            }
          }
          if (disjunction == null) {
            return Bpl.Expr.False;
          } else {
            return disjunction;
          }
        }
        return Bpl.Expr.SelectTok(tok, TrExpr(s), BoxIfNecessary(tok, elmt, elmtType));
      }
      
      Bpl.QKeyValue TrAttributes(Attributes attrs) {
        Bpl.QKeyValue kv = null;
        while (attrs != null) {
          List<object> parms = new List<object>();
          foreach (Attributes.Argument arg in attrs.Args) {
            if (arg.E != null) {
              parms.Add(TrExpr(arg.E));
            } else {
              parms.Add(cce.NonNull(arg.S));
            }
          }
          kv = new Bpl.QKeyValue(Token.NoToken, attrs.Name, parms, kv);
          attrs = attrs.Prev;
        }
        return kv;
      }
      
      // --------------- help routines ---------------

      public Bpl.Expr IsAlloced(IToken tok, Bpl.Expr e) {
        Contract.Requires(tok != null);
        Contract.Requires(e != null);
        Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

        return IsAlloced(tok, e, HeapExpr);
      }
          
      Bpl.Expr IsAlloced(IToken tok, Bpl.Expr e, Bpl.Expr heap) {
        Contract.Requires(tok != null);
        Contract.Requires(e != null);
        Contract.Requires(heap != null);
        Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

        return Bpl.Expr.SelectTok(tok, heap, e, predef.Alloc(tok));
      }
      
      public Bpl.Expr GoodRef(IToken tok, Bpl.Expr e, Type type) {
        Contract.Requires(tok != null);
        Contract.Requires(e != null);
        Contract.Requires(type != null);
        Contract.Ensures(Contract.Result<Bpl.Expr>() != null);

        if (type is UserDefinedType && ((UserDefinedType)type).ResolvedClass != null) {
          // Heap[e, alloc] && dtype(e) == T
          return GoodRef_Class(tok, e, (UserDefinedType)type, false);
        } else {
          // Heap[e, alloc]
          return IsAlloced(tok, e);
        }
      }
      
      public Bpl.Expr GoodRef_Class(IToken tok, Bpl.Expr e, UserDefinedType type, bool isNew)
       {
        Contract.Requires(tok != null);
        Contract.Requires(e != null);
        Contract.Requires(type != null);
        Contract.Requires( type.ResolvedClass is ClassDecl);
        Contract.Ensures(Contract.Result<Bpl.Expr>() != null);
        return GoodRef_Ref(tok, e, new Bpl.IdentifierExpr(tok, translator.GetClass(type.ResolvedClass)), type.TypeArgs, isNew);
      }
      
      public Bpl.Expr GoodRef_Ref(IToken tok, Bpl.Expr e, Bpl.Expr type, List<Type/*!*/>/*!*/ typeArgs, bool isNew) {
        Contract.Requires(tok != null);
        Contract.Requires(e != null);
        Contract.Requires(type != null);
        Contract.Requires(cce.NonNullElements(typeArgs));
        // Heap[e, alloc]
        Bpl.Expr r = IsAlloced(tok, e);
        if (isNew) {
          r = Bpl.Expr.Not(r);  // use the conjunct:  !Heap[e, alloc]
        }
        
        // dtype(e) == C
        Bpl.Expr dtypeFunc = translator.FunctionCall(tok, BuiltinFunction.DynamicType, null, e);
        Bpl.Expr dtype = Bpl.Expr.Eq(dtypeFunc, type);
        r = Bpl.Expr.And(r, dtype);
        
        // dtypeParams(e, #) == T
        int n = 0;
        foreach (Type arg in typeArgs) {
          Bpl.Expr tpFunc = translator.FunctionCall(tok, BuiltinFunction.TypeParams, null, e, Bpl.Expr.Literal(n));
          Bpl.Expr ta = translator.GetTypeExpr(tok, arg);
          if (ta != null) {
            r = Bpl.Expr.And(r, Bpl.Expr.Eq(tpFunc, ta));
          }
          n++;
        }
        
        return r;
      }
      
      public Bpl.Expr TypeAlloced(IToken tok, Bpl.Expr e, Type type) {
        Contract.Requires(tok != null);
        Contract.Requires(e!= null);
        Contract.Requires(type != null);
        while (true) {
          TypeProxy proxy = type as TypeProxy;
          if (proxy == null) {
            break;
          } else if (proxy.T == null) {
            return null;
          } else {
            type = proxy.T;
          }
        }
        
        Bpl.BoundVariable bv = null;
        Bpl.Expr ante = null;
        if (type.IsRefType) {  // object or class type
          // e == null || $Heap[e, alloc]
          // This is done below
          
        } else if (type is SetType) {
          // (forall tp: BoxType :: e[tp] ==> Unbox(tp) == null || $Heap[Unbox(tp), alloc])
          bv = new Bpl.BoundVariable(tok, new Bpl.TypedIdent(tok, "$tp", predef.BoxType));
          Bpl.Expr tp = new Bpl.IdentifierExpr(tok, bv);
          ante = Bpl.Expr.SelectTok(tok, e, tp);  // note, this means the set-inclusion does not undergo the set-inclusion expansion optimization done by TrInSet
          e = translator.FunctionCall(tok, BuiltinFunction.Unbox, predef.RefType, tp);
          
        } else if (type is SeqType) {
          // (forall i: int :: Unbox(Seq#Index(e,i)) == null || $Heap[Unbox(Seq#Index(e,i)), alloc])
          bv = new Bpl.BoundVariable(tok, new Bpl.TypedIdent(tok, "$i", Bpl.Type.Int));
          e = translator.FunctionCall(tok, BuiltinFunction.SeqIndex, predef.BoxType, e, new Bpl.IdentifierExpr(tok, bv));
          e = translator.FunctionCall(tok, BuiltinFunction.Unbox, predef.RefType, e);
          
        } else {
          return null;
        }
        
        Bpl.Expr r = Bpl.Expr.Or(Bpl.Expr.Eq(e, predef.Null), GoodRef(tok, e, type));
        if (ante != null) {
          r = Bpl.Expr.Imp(ante, r);
        }
        if (bv != null) {
          r = new Bpl.ForallExpr(tok, new Bpl.VariableSeq(bv), r);
        }
        return r;
      }
    }
    
    enum BuiltinFunction {
      SetEmpty,
      SetUnionOne,
      SetUnion,
      SetIntersection,
      SetDifference,
      SetEqual,
      SetSubset,
      SetDisjoint,
      
      SeqLength,
      SeqEmpty,
      SeqBuild,
      SeqAppend,
      SeqIndex,
      SeqUpdate,
      SeqContains,
      SeqDrop,
      SeqTake,
      SeqEqual,
      SeqSameUntil,
      
      ArrayLength,
      IndexField,
      
      IfThenElse,
      
      Box,
      Unbox,
      
      IsGoodHeap,
      HeapSucc,
      
      DynamicType,  // allocated type
      TypeParams,   // type parameters to allocated type
      TypeTuple,
      DeclType,
      FCat,  // field category (indexed or named)
      
      DatatypeCtorId,
      DtRank,
      
      // CEV
      CevInit,
      CevVarIntro,
      CevVarUpdate,
      CevControlFlowEvent,
      CevProgramLocation,
    }
    
    // The "typeInstantiation" argument is passed in to help construct the result type of the function.
    Bpl.NAryExpr FunctionCall(IToken tok, BuiltinFunction f, Bpl.Type typeInstantiation, params Bpl.Expr[] args)
     {
      Contract.Requires(tok != null);
      Contract.Requires(args != null);
     Contract.Requires( predef != null);
     Contract.Ensures(Contract.Result<Bpl.NAryExpr>() != null);

      switch (f) {
        case BuiltinFunction.SetEmpty: {
          Contract.Assert( args.Length == 0);
          Contract.Assert( typeInstantiation != null);
          Bpl.Type resultType = predef.SetType(tok, typeInstantiation);
          return Bpl.Expr.CoerceType(tok, FunctionCall(tok, "Set#Empty", resultType, args), resultType);
        }
        case BuiltinFunction.SetUnionOne:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "Set#UnionOne", predef.SetType(tok, typeInstantiation), args);
        case BuiltinFunction.SetUnion:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "Set#Union", predef.SetType(tok, typeInstantiation), args);
        case BuiltinFunction.SetIntersection:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "Set#Intersection", predef.SetType(tok, typeInstantiation), args);
        case BuiltinFunction.SetDifference:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "Set#Difference", predef.SetType(tok, typeInstantiation), args);
        case BuiltinFunction.SetEqual:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "Set#Equal", Bpl.Type.Bool, args);
        case BuiltinFunction.SetSubset:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "Set#Subset", Bpl.Type.Bool, args);
        case BuiltinFunction.SetDisjoint:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "Set#Disjoint", Bpl.Type.Bool, args);

        case BuiltinFunction.SeqLength:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "Seq#Length", Bpl.Type.Int, args);
        case BuiltinFunction.SeqEmpty: {
          Contract.Assert( args.Length == 0);
          Contract.Assert( typeInstantiation != null);
          Bpl.Type resultType = predef.SeqType(tok, typeInstantiation);
          return Bpl.Expr.CoerceType(tok, FunctionCall(tok, "Seq#Empty", resultType, args), resultType);
        }
        case BuiltinFunction.SeqBuild:
          Contract.Assert( args.Length == 4);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "Seq#Build", predef.SeqType(tok, typeInstantiation), args);
        case BuiltinFunction.SeqAppend:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "Seq#Append", predef.SeqType(tok, typeInstantiation), args);
        case BuiltinFunction.SeqIndex:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "Seq#Index", typeInstantiation, args);
        case BuiltinFunction.SeqUpdate:
          Contract.Assert( args.Length == 3);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "Seq#Update", predef.SeqType(tok, typeInstantiation), args);
        case BuiltinFunction.SeqContains:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "Seq#Contains", Bpl.Type.Bool, args);
        case BuiltinFunction.SeqDrop:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "Seq#Drop", predef.SeqType(tok, typeInstantiation), args);
        case BuiltinFunction.SeqTake:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "Seq#Take", predef.SeqType(tok, typeInstantiation), args);
        case BuiltinFunction.SeqEqual:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "Seq#Equal", Bpl.Type.Bool, args);
        case BuiltinFunction.SeqSameUntil:
          Contract.Assert( args.Length == 3);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "Seq#SameUntil", Bpl.Type.Bool, args);
          
        case BuiltinFunction.ArrayLength:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "Array#Length", Bpl.Type.Int, args);
        case BuiltinFunction.IndexField:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "IndexField", predef.FieldName(tok, predef.BoxType), args);

        case BuiltinFunction.IfThenElse:
          Contract.Assert( args.Length == 3);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "$ite", typeInstantiation, args);

        case BuiltinFunction.Box:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "$Box", predef.BoxType, args);
        case BuiltinFunction.Unbox:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation != null);
          return Bpl.Expr.CoerceType(tok, FunctionCall(tok, "$Unbox", typeInstantiation, args), typeInstantiation);

        case BuiltinFunction.IsGoodHeap:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "$IsGoodHeap", Bpl.Type.Bool, args);
        case BuiltinFunction.HeapSucc:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "$HeapSucc", Bpl.Type.Bool, args);

        case BuiltinFunction.DynamicType:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "dtype", predef.ClassNameType, args);
        case BuiltinFunction.TypeParams:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "TypeParams", predef.ClassNameType, args);
        case BuiltinFunction.TypeTuple:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "TypeTuple", predef.ClassNameType, args);
        case BuiltinFunction.DeclType:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "DeclType", predef.ClassNameType, args);
        case BuiltinFunction.FCat:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation != null);
          return FunctionCall(tok, "FCat", predef.FieldCategoryType, args);
          
        case BuiltinFunction.DatatypeCtorId:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "DatatypeCtorId", predef.DtCtorId, args);
        case BuiltinFunction.DtRank:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "DtRank", Bpl.Type.Int, args);
          
        case BuiltinFunction.CevInit:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "#cev_init", Bpl.Type.Bool, args);
        case BuiltinFunction.CevVarIntro:
          Contract.Assert( args.Length == 5);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "#cev_var_intro", Bpl.Type.Bool, args);
        case BuiltinFunction.CevVarUpdate:
          Contract.Assert( args.Length == 4);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "#cev_var_update", Bpl.Type.Bool, args);
        case BuiltinFunction.CevControlFlowEvent:
          Contract.Assert( args.Length == 2);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "#cev_control_flow_event", Bpl.Type.Bool, args);
        case BuiltinFunction.CevProgramLocation:
          Contract.Assert( args.Length == 1);
          Contract.Assert( typeInstantiation == null);
          return FunctionCall(tok, "#cev_save_position", predef.CevTokenType, args);
           
        default:
          Contract.Assert(false); throw new cce.UnreachableException();  // unexpected built-in function
      }
    }
    
    Bpl.NAryExpr FunctionCall(IToken tok, string function, Bpl.Type returnType, params Bpl.Expr[] args)
    {
      Contract.Requires(tok != null);
      Contract.Requires(function != null);
      Contract.Requires(args != null);
      Contract.Ensures(Contract.Result<Bpl.NAryExpr>() != null);

      return new Bpl.NAryExpr(tok, new Bpl.FunctionCall(new Bpl.IdentifierExpr(tok, function, returnType)), new Bpl.ExprSeq(args));
    }

    Bpl.NAryExpr FunctionCall(IToken tok, string function, Bpl.Type returnType, List<Bpl.Expr> args)
    {
      Contract.Requires(tok != null);
      Contract.Requires(function != null);
      Contract.Requires(returnType != null);
      Contract.Requires(cce.NonNullElements(args));


    Contract.Ensures(Contract.Result<Bpl.NAryExpr>() != null);

      Bpl.ExprSeq aa = new Bpl.ExprSeq();
      foreach (Bpl.Expr arg in args) {
        aa.Add(arg);
      }
      return new Bpl.NAryExpr(tok, new Bpl.FunctionCall(new Bpl.IdentifierExpr(tok, function, returnType)), aa);
    }

    public bool SplitExpr(Expression expr, out List<Expression/*!*/>/*!*/ definitions, out List<Expression/*!*/>/*!*/ pieces) {
      Contract.Requires(expr != null);
      Contract.Ensures(cce.NonNullElements(Contract.ValueAtReturn(out definitions)));
      Contract.Ensures(cce.NonNullElements(Contract.ValueAtReturn(out pieces)));
      definitions = new List<Expression>();
      pieces = new List<Expression>();
      return SplitExpr(expr, true, definitions, pieces);
    }

    ///<summary>
    /// Returns false if no split occurred (in that case, nothing was added to definitions, and (exactly) expr itself was added to pieces.
    ///</summary>
    public bool SplitExpr(Expression expr, bool expandFunctions, List<Expression/*!*/>/*!*/ definitions, List<Expression/*!*/>/*!*/ pieces)
     {
      Contract.Requires(expr != null);
      Contract.Requires(cce.NonNullElements(definitions));
      Contract.Requires(cce.NonNullElements(pieces));
     Contract.Requires( expr.Type is BoolType || (expr is BoxingCastExpr && ((BoxingCastExpr)expr).E.Type is BoolType));
      if (expr is BoxingCastExpr) {
        BoxingCastExpr bce = (BoxingCastExpr)expr;
        List<Expression> pp = new List<Expression>();
        if (SplitExpr(bce.E, expandFunctions, definitions, pp)) {
          foreach (Expression e in pp) {
            Expression r = new BoxingCastExpr(e, bce.FromType, bce.ToType);
            r.Type = bce.ToType;  // resolve here
            pieces.Add(r);
          }
          return true;
        }
        
      } else if (expr is BinaryExpr) {
        BinaryExpr bin = (BinaryExpr)expr;
        if (bin.ResolvedOp == BinaryExpr.ResolvedOpcode.And) {
          SplitExpr(bin.E0, expandFunctions, definitions, pieces);
          SplitExpr(bin.E1, expandFunctions, definitions, pieces);
          return true;
        
        } else if (bin.ResolvedOp == BinaryExpr.ResolvedOpcode.Imp) {
          List<Expression> pp = new List<Expression>();
          if (SplitExpr(bin.E1, expandFunctions, definitions, pp)) {
            foreach (Expression e in pp) {
              BinaryExpr r = new BinaryExpr(e.tok, bin.Op, bin.E0, e);
              r.ResolvedOp = bin.ResolvedOp;  r.Type = bin.Type;  // resolve on the fly
              pieces.Add(r);
            }
            return true;
          }
        }
      
      } else if (expr is ITEExpr) {
        ITEExpr ite = (ITEExpr)expr;

        List<Expression> pp = new List<Expression>();
        SplitExpr(ite.Thn, expandFunctions, definitions, pp);
        foreach (Expression e in pp) {
          BinaryExpr r = new BinaryExpr(e.tok, BinaryExpr.Opcode.Imp, ite.Test, e);
          r.ResolvedOp = BinaryExpr.ResolvedOpcode.Imp;  r.Type = ite.Type;  // resolve on the fly
          pieces.Add(r);
        }

        Expression negatedGuard = new UnaryExpr(ite.Test.tok, UnaryExpr.Opcode.Not, ite.Test);
        negatedGuard.Type = ite.Test.Type;  // resolve on the fly
        pp = new List<Expression>();
        SplitExpr(ite.Els, expandFunctions, definitions, pp);
        foreach (Expression e in pp) {
          BinaryExpr r = new BinaryExpr(e.tok, BinaryExpr.Opcode.Imp, negatedGuard, e);
          r.ResolvedOp = BinaryExpr.ResolvedOpcode.Imp;  r.Type = ite.Type;  // resolve on the fly
          pieces.Add(r);
        }
        return true;
      
      } else if (expr is OldExpr) {
        List<Expression> dd = new List<Expression>();
        List<Expression> pp = new List<Expression>();
        if (SplitExpr(((OldExpr)expr).E, expandFunctions, dd, pp)) {
          foreach (Expression e in dd) {
            Expression r = new OldExpr(expr.tok, e);
            r.Type = Type.Bool;  // resolve here
            definitions.Add(r);
          }
          foreach (Expression e in pp) {
            Expression r = new OldExpr(expr.tok, e);
            r.Type = e.Type;  // resolve here
            pieces.Add(r);
          }
          return true;
        }
        
      } else if (expandFunctions && expr is FunctionCallExpr) {
        FunctionCallExpr fexp = (FunctionCallExpr)expr;
        Contract.Assert( fexp.Function != null);  // filled in during resolution
        if (fexp.Function.Body != null && !(fexp.Function.Body is MatchExpr)) {
          // inline this body
          Dictionary<IVariable,Expression> substMap = new Dictionary<IVariable,Expression>();
          Contract.Assert( fexp.Args.Count == fexp.Function.Formals.Count);
          for (int i = 0; i < fexp.Function.Formals.Count; i++) {
            Formal p = fexp.Function.Formals[i];
            Expression arg = fexp.Args[i];
            arg = new BoxingCastExpr(arg, cce.NonNull(arg.Type), p.Type);
            arg.Type = p.Type;  // resolve here
            substMap.Add(p, arg);
          }
          Expression body = Substitute(fexp.Function.Body, fexp.Receiver, substMap);

          // add definition:  fn(args) ==> body
          Expression bodyx = new UnboxingCastExpr(body, fexp.Function.ResultType, cce.NonNull(expr.Type));
          bodyx.Type = expr.Type;  // resolve here
          BinaryExpr def = new BinaryExpr(expr.tok, BinaryExpr.Opcode.Imp, fexp, bodyx);
          def.ResolvedOp = BinaryExpr.ResolvedOpcode.Imp;  def.Type = Type.Bool;  // resolve on the fly
          definitions.Add(def);

          // recurse on body
          List<Expression> pp = new List<Expression>();
          SplitExpr(body, false, definitions, pp);
          foreach (Expression e in pp) {
            Expression r = new UnboxingCastExpr(e, fexp.Function.ResultType, expr.Type);
            r.Type = expr.Type;  // resolve here
            pieces.Add(r);
          }
          return true;
        }
      }

      pieces.Add(expr);
      return false;
    }
    
    static Expression Substitute(Expression expr, Expression receiverReplacement, Dictionary<IVariable,Expression/*!*/>/*!*/ substMap) {
      Contract.Requires(expr != null);
      Contract.Requires(cce.NonNullElements(substMap));
      Contract.Ensures(Contract.Result<Expression>() != null);

      Expression newExpr = null;  // set to non-null value only if substitution has any effect; if non-null, newExpr will be resolved at end
      
      if (expr is LiteralExpr || expr is WildcardExpr) {
        // nothing to substitute
      } else if (expr is ThisExpr) {
        return receiverReplacement == null ? expr : receiverReplacement;
      } else if (expr is IdentifierExpr) {
        IdentifierExpr e = (IdentifierExpr)expr;
        Expression substExpr;
        if (substMap.TryGetValue(e.Var, out substExpr)) {
          return cce.NonNull(substExpr);
        }
      } else if (expr is DisplayExpression) {
        DisplayExpression e = (DisplayExpression)expr;
        List<Expression> newElements = SubstituteExprList(e.Elements, receiverReplacement, substMap);
        if (newElements != e.Elements) {
          if (expr is SetDisplayExpr) {
            newExpr = new SetDisplayExpr(expr.tok, newElements);
          } else {
            newExpr = new SeqDisplayExpr(expr.tok, newElements);
          }
        }
        
      } else if (expr is FieldSelectExpr) {
        FieldSelectExpr fse = (FieldSelectExpr)expr;
        Expression substE = Substitute(fse.Obj, receiverReplacement, substMap);
        if (substE != fse.Obj) {
          FieldSelectExpr fseNew = new FieldSelectExpr(fse.tok, substE, fse.FieldName);
          fseNew.Field = fse.Field;  // resolve on the fly (and fseExpr.Type is set at end of method)
          newExpr = fseNew;
        }
        
      } else if (expr is SeqSelectExpr) {
        SeqSelectExpr sse = (SeqSelectExpr)expr;
        Expression seq = Substitute(sse.Seq, receiverReplacement, substMap);
        Expression e0 = sse.E0 == null ? null : Substitute(sse.E0, receiverReplacement, substMap);
        Expression e1 = sse.E1 == null ? null : Substitute(sse.E1, receiverReplacement, substMap);
        if (seq != sse.Seq || e0 != sse.E0 || e1 != sse.E1) {
          newExpr = new SeqSelectExpr(sse.tok, sse.SelectOne, seq, e0, e1);
        }
        
      } else if (expr is SeqUpdateExpr) {
        SeqUpdateExpr sse = (SeqUpdateExpr)expr;
        Expression seq = Substitute(sse.Seq, receiverReplacement, substMap);
        Expression index = Substitute(sse.Index, receiverReplacement, substMap);
        Expression val = Substitute(sse.Value, receiverReplacement, substMap);
        if (seq != sse.Seq || index != sse.Index || val != sse.Value) {
          newExpr = new SeqUpdateExpr(sse.tok, seq, index, val);
        }
        
      } else if (expr is FunctionCallExpr) {
        FunctionCallExpr e = (FunctionCallExpr)expr;
        Expression receiver = Substitute(e.Receiver, receiverReplacement, substMap);
        List<Expression> newArgs = SubstituteExprList(e.Args, receiverReplacement, substMap);
        if (receiver != e.Receiver || newArgs != e.Args) {
          FunctionCallExpr newFce = new FunctionCallExpr(expr.tok, e.Name, receiver, newArgs);
          newFce.Function = e.Function;  // resolve on the fly (and set newFce.Type below, at end)
          newExpr = newFce;
        }
      
      } else if (expr is DatatypeValue) {
        DatatypeValue dtv = (DatatypeValue)expr;
        List<Expression> newArgs = SubstituteExprList(dtv.Arguments, receiverReplacement, substMap);
        if (newArgs != dtv.Arguments) {
          DatatypeValue newDtv = new DatatypeValue(dtv.tok, dtv.DatatypeName, dtv.MemberName, newArgs);
          newDtv.Ctor = dtv.Ctor;  // resolve on the fly (and set newDtv.Type below, at end)
          newExpr = newDtv;
        }

      } else if (expr is OldExpr) {
        OldExpr e = (OldExpr)expr;
        Expression se = Substitute(e.E, receiverReplacement, substMap);
        if (se != e.E) {
          newExpr = new OldExpr(expr.tok, se);
        }
      } else if (expr is FreshExpr) {
        FreshExpr e = (FreshExpr)expr;
        Expression se = Substitute(e.E, receiverReplacement, substMap);
        if (se != e.E) {
          newExpr = new FreshExpr(expr.tok, se);
        }
      } else if (expr is UnaryExpr) {
        UnaryExpr e = (UnaryExpr)expr;
        Expression se = Substitute(e.E, receiverReplacement, substMap);
        if (se != e.E) {
          newExpr = new UnaryExpr(expr.tok, e.Op, se);
        }
      } else if (expr is BinaryExpr) {
        BinaryExpr e = (BinaryExpr)expr;
        Expression e0 = Substitute(e.E0, receiverReplacement, substMap);
        Expression e1 = Substitute(e.E1, receiverReplacement, substMap);
        if (e0 != e.E0 || e1 != e.E1) {
          BinaryExpr newBin = new BinaryExpr(expr.tok, e.Op, e0, e1);
          newBin.ResolvedOp = e.ResolvedOp;  // part of what needs to be done to resolve on the fly (newBin.Type is set below, at end)
          newExpr = newBin;
        }
        
      } else if (expr is QuantifierExpr) {
        QuantifierExpr e = (QuantifierExpr)expr;
        Expression newBody = Substitute(e.Body, receiverReplacement, substMap);
        Triggers newTrigs = SubstTriggers(e.Trigs, receiverReplacement, substMap);
        Attributes newAttrs = SubstAttributes(e.Attributes, receiverReplacement, substMap);
        if (newBody != e.Body || newTrigs != e.Trigs || newAttrs != e.Attributes) {
          if (expr is ForallExpr) {
            newExpr = new ForallExpr(expr.tok, e.BoundVars, newBody, newTrigs, newAttrs);
          } else {
            newExpr = new ExistsExpr(expr.tok, e.BoundVars, newBody, newTrigs, newAttrs);
          }
        }
        
      } else if (expr is ITEExpr) {
        ITEExpr e = (ITEExpr)expr;
        Expression test = Substitute(e.Test, receiverReplacement, substMap);
        Expression thn = Substitute(e.Thn, receiverReplacement, substMap);
        Expression els = Substitute(e.Els, receiverReplacement, substMap);
        if (test != e.Test || thn != e.Thn || els != e.Els) {
          newExpr = new ITEExpr(expr.tok, test, thn, els);
        }
      }
      
      if (newExpr == null) {
        return expr;
      } else {
        newExpr.Type = expr.Type;  // resolve on the fly (any additional resolution must be done above)
        return newExpr;
      }
    }
    
    static List<Expression/*!*/>/*!*/ SubstituteExprList(List<Expression/*!*/>/*!*/ elist,
                                                 Expression receiverReplacement, Dictionary<IVariable,Expression/*!*/>/*!*/ substMap) {
      Contract.Requires(cce.NonNullElements(elist));
      Contract.Requires(cce.NonNullElements(substMap));
      Contract.Ensures(cce.NonNullElements(Contract.Result<List<Expression>>()));
      List<Expression> newElist = null;  // initialized lazily
      for (int i = 0; i < elist.Count; i++)
      {cce.LoopInvariant(  newElist == null || newElist.Count == i);
      
        Expression substE = Substitute(elist[i], receiverReplacement, substMap);
        if (substE != elist[i] && newElist == null) {
          newElist = new List<Expression>();
          for (int j = 0; j < i; j++) {
            newElist.Add(elist[j]);
          }
        }
        if (newElist != null) {
          newElist.Add(substE);
        }
      }
      if (newElist == null) {
        return elist;
      } else {
        return newElist;
      }
    }
    
    static Triggers SubstTriggers(Triggers trigs, Expression receiverReplacement, Dictionary<IVariable,Expression/*!*/>/*!*/ substMap) {
      Contract.Requires(cce.NonNullElements(substMap));
      if (trigs != null) {
        List<Expression> terms = SubstituteExprList(trigs.Terms, receiverReplacement, substMap);
        Triggers prev = SubstTriggers(trigs.Prev, receiverReplacement, substMap);
        if (terms != trigs.Terms || prev != trigs.Prev) {
          return new Triggers(terms, prev);
        }
      }
      return trigs;
    }

    static Attributes SubstAttributes(Attributes attrs, Expression receiverReplacement, Dictionary<IVariable, Expression/*!*/>/*!*/ substMap) {
      Contract.Requires(cce.NonNullElements(substMap));
      if (attrs != null) {
        List<Attributes.Argument> newArgs = new List<Attributes.Argument>();  // allocate it eagerly, what the heck, it doesn't seem worth the extra complexity in the code to do it lazily for the infrequently occurring attributes
        bool anyArgSubst = false;
        foreach (Attributes.Argument arg in attrs.Args) {
          Attributes.Argument newArg = arg;
          if (arg.E != null) {
            Expression newE = Substitute(arg.E, receiverReplacement, substMap);
            if (newE != arg.E) {
              newArg = new Attributes.Argument(newE);
              anyArgSubst = true;
            }
          }
          newArgs.Add(newArg);
        }
        if (!anyArgSubst) {
          newArgs = attrs.Args;
        }
        
        Attributes prev = SubstAttributes(attrs.Prev, receiverReplacement, substMap);
        if (newArgs != attrs.Args || prev != attrs.Prev) {
          return new Attributes(attrs.Name, newArgs, prev);
        }
      }
      return attrs;
    }
        
  }
}