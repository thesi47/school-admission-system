// Sprint 0: only proves that React, Tailwind CSS and DaisyUI work.
// The real pages and the router come in Sprint 5.
export default function App() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-base-200">
      <div className="card bg-base-100 shadow">
        <div className="card-body items-center text-center">
          <h1 className="card-title">School Admission System</h1>
          <p>The frontend is set up. Tailwind CSS and DaisyUI are ready.</p>
          <button type="button" className="btn btn-primary">
            DaisyUI button
          </button>
        </div>
      </div>
    </div>
  )
}
